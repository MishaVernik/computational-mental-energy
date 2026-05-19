namespace Cme.Core;

/// <summary>
/// Represents a single EEG window record from Excel.
/// </summary>
public class EegWindowRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? TaskId { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    // EEG band powers
    public double DeltaPower { get; set; }
    public double ThetaPower { get; set; }
    public double AlphaPower { get; set; }
    public double BetaPower { get; set; }

    // Cognitive features
    public double ComplexityIndex { get; set; } = 0.5; // Default if missing
    public double FlowProbability { get; set; } = 0.0; // Default if missing

    // Optional fields (passed through)
    public double? ArtifactScore { get; set; }
    public bool? IsArtifact { get; set; }
    public Dictionary<string, object> ExtraColumns { get; set; } = new();

    /// <summary>
    /// Calculate window duration in seconds.
    /// </summary>
    public double GetDeltaSeconds()
    {
        if (StartUtc.HasValue && EndUtc.HasValue)
        {
            return (EndUtc.Value - StartUtc.Value).TotalSeconds;
        }
        return 5.0; // Default window duration
    }
}


