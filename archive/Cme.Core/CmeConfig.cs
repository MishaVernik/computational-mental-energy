namespace Cme.Core;

/// <summary>
/// Configuration for CME calculation parameters.
/// </summary>
public class CmeConfig
{
    // Energy weights per frequency band
    public double WDelta { get; set; } = 0.5;
    public double WTheta { get; set; } = 1.0;
    public double WAlpha { get; set; } = 1.0;
    public double WBeta { get; set; } = 0.3;
    public double WGamma { get; set; } = 0.0; // optional; set > 0 when γ-band data is available

    // Modulation function g(c,p) = λ1·c + λ2·p + λ3·c·p
    public double Lambda1 { get; set; } = 0.5;  // c coefficient
    public double Lambda2 { get; set; } = 0.5;  // p coefficient
    public double Lambda3 { get; set; } = 0.5;  // c*p coefficient

    // Flow detection threshold (configurable; may be set via calibration)
    public double FlowThreshold { get; set; } = 0.85;

    // κ: dimensionless scaling factor (overridden by calibration/personal profile)
    public double Kappa { get; set; } = 10.0;

    // Target for CME_index display scale: CME_index = CME_rate / CME_rate_max_cal * MaxCmeTarget
    public double MaxCmeTarget { get; set; } = 100.0;

    // Calibration: number of clean windows to collect before computing κ
    public int CalibrationWindows { get; set; } = 24;

    // Calibration: target max CME_rate for κ computation (κ = CalibrationTarget / max(CME_rate_raw))
    public double CalibrationTarget { get; set; } = 100.0;

    public static CmeConfig LoadFromFile(string? configPath)
    {
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            return new CmeConfig(); // Return defaults
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(json);
            return config ?? new CmeConfig();
        }
        catch
        {
            return new CmeConfig(); // Return defaults on error
        }
    }

    public void SaveToFile(string configPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }
}

