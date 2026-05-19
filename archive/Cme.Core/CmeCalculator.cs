namespace Cme.Core;

/// <summary>
/// Calculates CME (Countable Mental Energy) using the specified formula.
/// </summary>
public class CmeCalculator
{
    private readonly CmeConfig _config;

    public CmeCalculator(CmeConfig? config = null)
    {
        _config = config ?? new CmeConfig();
    }

    /// <summary>
    /// Calculate EEG band energy E_band(t).
    /// </summary>
    public double CalculateEnergy(EegWindowRecord window)
    {
        return _config.WDelta * window.DeltaPower
             + _config.WTheta * window.ThetaPower
             + _config.WAlpha * window.AlphaPower
             + _config.WBeta * window.BetaPower;
    }

    /// <summary>
    /// Calculate modulation function g(c, p).
    /// </summary>
    public double CalculateModulation(double complexity, double flowProbability)
    {
        return _config.Lambda1 * complexity
             + _config.Lambda2 * flowProbability
             + _config.Lambda3 * complexity * flowProbability;
    }

    /// <summary>
    /// Calculate raw CME (before normalization).
    /// </summary>
    public double CalculateRawCme(EegWindowRecord window)
    {
        double eBand = CalculateEnergy(window);
        double g = CalculateModulation(window.ComplexityIndex, window.FlowProbability);
        double delta = window.GetDeltaSeconds();

        return eBand * g * delta;
    }

    /// <summary>
    /// Calculate normalized CME for a window.
    /// </summary>
    public double CalculateCme(EegWindowRecord window, double normalizationFactor)
    {
        double rawCme = CalculateRawCme(window);
        return normalizationFactor * rawCme;
    }

    /// <summary>
    /// Calculate normalization factor k such that max CME ≈ target.
    /// </summary>
    public double CalculateNormalizationFactor(List<EegWindowRecord> windows, double targetMax = 100.0)
    {
        if (windows.Count == 0) return 1.0;

        double maxRaw = windows.Max(w => CalculateRawCme(w));
        
        if (maxRaw > 0)
        {
            return targetMax / maxRaw;
        }

        return 1.0;
    }

    /// <summary>
    /// Check if window is in flow state.
    /// </summary>
    public bool IsFlowWindow(EegWindowRecord window)
    {
        return window.FlowProbability >= _config.FlowThreshold;
    }
}


