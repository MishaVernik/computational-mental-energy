using Cme.Core;

namespace CmeSim.Api.Services;

/// <summary>
/// Physiological plausibility thresholds for EEG band powers (linear μV²,
/// per electrode, integrated Welch PSD, consumer dry-electrode devices).
/// Sources: Katahira 2018, Raufi & Longo 2022, Pope 1995, artifact literature.
/// </summary>
public static class EegLimits
{
    public static readonly (double Clean, double Artifact, double Reject) Delta = (30, 100, 500);
    public static readonly (double Clean, double Artifact, double Reject) Theta = (20, 50, 200);
    public static readonly (double Clean, double Artifact, double Reject) Alpha = (50, 100, 500);
    public static readonly (double Clean, double Artifact, double Reject) Beta  = (15, 50, 200);
    public static readonly (double Clean, double Artifact, double Reject) Gamma = (5, 20, 100);
    public static readonly double TotalPowerReject = 1000;

    public static double Clamp(double value, double artifactThreshold)
        => Math.Min(Math.Max(value, 0), artifactThreshold);

    public static bool IsChannelReject(double delta, double theta, double alpha, double beta, double gamma)
        => delta > Delta.Reject || theta > Theta.Reject || alpha > Alpha.Reject
        || beta > Beta.Reject || gamma > Gamma.Reject
        || (delta + theta + alpha + beta + gamma) > TotalPowerReject;

    public static bool IsAllClean(double delta, double theta, double alpha, double beta, double gamma)
        => delta <= Delta.Clean && theta <= Theta.Clean && alpha <= Alpha.Clean
        && beta <= Beta.Clean && gamma <= Gamma.Clean;
}

/// <summary>
/// Immutable calibration context: κ, per-feature min/max from a completed calibration.
/// Passed into stateless ComputeCme so the calculator holds no mutable state.
/// </summary>
public record CalibrationContext(double Kappa, double[] FeatureMin, double[] FeatureMax);

/// <summary>
/// CME_rate(t) = κ · E_band(t) · g(c(t), p_focus(t))   [Вн/s]
/// CME(t) = CME_rate(t) · Δ                              [Вн]
/// CME_index(t) = CME_rate(t) / CME_rate_max_cal * 100   [dimensionless]
/// 1 Вн ≡ 1 μV²·s
/// </summary>
public record CmeComputeResult(double CmeVn, double CmeIndex, double CmeRate);

public interface ICmeCalculator
{
    CmeComputeResult ComputeCme(double[] features, double pFlow, double taskDifficulty,
        double deltaSeconds = 5.0, CalibrationContext? calibration = null);
}

public class CmeCalculator : ICmeCalculator
{
    private readonly ILogger<CmeCalculator> _logger;
    private readonly CmeConfig _cfg;

    public CmeCalculator(ILogger<CmeCalculator> logger)
    {
        _logger = logger;
        _cfg = new CmeConfig();
    }

    public CmeCalculator(ILogger<CmeCalculator> logger, CmeConfig config)
    {
        _logger = logger;
        _cfg = config;
    }

    public CmeComputeResult ComputeCme(double[] features, double pFlow, double taskDifficulty,
        double deltaSeconds = 5.0, CalibrationContext? calibration = null)
    {
        double energy = CalculateEnergy(features);
        double g = CalculateModulation(taskDifficulty, pFlow);
        double kappa = calibration?.Kappa ?? _cfg.Kappa;
        double cmeRate = kappa * energy * g;
        double cmeVn = cmeRate * deltaSeconds;

        double cmeIndex = calibration != null
            ? Math.Min(cmeRate / (kappa > 1e-12 ? _cfg.CalibrationTarget : 1.0) * _cfg.MaxCmeTarget, _cfg.MaxCmeTarget)
            : 0;

        _logger.LogDebug(
            "CME: E_band={Energy:F3} μV², g={G:F3}, κ={Kappa:F2}, CME_rate={Rate:F4} Вн/s, Δ={Delta}s → CME={CmeVn:F4} Вн, index={Index:F2}",
            energy, g, kappa, cmeRate, deltaSeconds, cmeVn, cmeIndex);

        return new CmeComputeResult(
            CmeVn: Math.Round(cmeVn, 4),
            CmeIndex: Math.Round(cmeIndex, 2),
            CmeRate: Math.Round(cmeRate, 4));
    }

    private double CalculateEnergy(double[] features)
    {
        if (features.Length < 4) return features.Sum(Math.Abs);
        double e = _cfg.WDelta * Math.Abs(features[0])
                 + _cfg.WTheta * Math.Abs(features[1])
                 + _cfg.WAlpha * Math.Abs(features[2])
                 + _cfg.WBeta * Math.Abs(features[3]);
        if (features.Length > 4)
            e += _cfg.WGamma * Math.Abs(features[4]);
        return e;
    }

    private double CalculateModulation(double c, double p)
    {
        return _cfg.Lambda1 * c + _cfg.Lambda2 * p + _cfg.Lambda3 * c * p;
    }
}
