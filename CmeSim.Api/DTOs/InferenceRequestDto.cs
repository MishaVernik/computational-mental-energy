namespace CmeSim.Api.DTOs;

/// <summary>
/// Request to compute CME for an EEG time window.
/// </summary>
public class InferenceRequestDto
{
    public string SessionId { get; set; } = string.Empty;
    public string WindowId { get; set; } = string.Empty;
    public double[] Features { get; set; } = Array.Empty<double>();
    public double TaskDifficulty { get; set; } // 0..1
}

/// <summary>
/// Response with computed CME and quantum metrics.
/// CmeVn is the base unit (1 Вн = 1 μV²·s).
/// CmeIndex is a dimensionless display scale [0..100].
/// </summary>
public class InferenceResponseDto
{
    public double CmeVn { get; set; }
    public double CmeIndex { get; set; }
    public double PFlow { get; set; }
    public int ShotsUsed { get; set; }
    public int Depth { get; set; }
    public int QpuLatencyMs { get; set; }
    public int TotalLatencyMs { get; set; }
}


