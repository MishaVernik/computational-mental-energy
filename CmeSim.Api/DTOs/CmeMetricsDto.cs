namespace CmeSim.Api.DTOs;

/// <summary>
/// Response DTO for CME metrics computation from Excel.
/// </summary>
public class CmeMetricsResponseDto
{
    public GlobalMetricsDto GlobalSummary { get; set; } = new();
    public List<SessionMetricsDto> SessionSummaries { get; set; } = new();
}

public class GlobalMetricsDto
{
    public int TotalSessions { get; set; }
    public double MeanCmeSession { get; set; }
    public double MedianCmeSession { get; set; }
    public double MeanFlowShare { get; set; }
    public int SessionsFlowShareGe05 { get; set; }
    public int SessionsFlowShareGe07 { get; set; }
    public double K { get; set; }
    public double WDelta { get; set; }
    public double WTheta { get; set; }
    public double WAlpha { get; set; }
    public double WBeta { get; set; }
    public double Lambda1 { get; set; }
    public double Lambda2 { get; set; }
    public double Lambda3 { get; set; }
    public double FlowThreshold { get; set; }
}

public class SessionMetricsDto
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TotalWindows { get; set; }
    public double TotalDurationSeconds { get; set; }
    public int FlowWindows { get; set; }
    public double FlowDurationSeconds { get; set; }
    public double FlowShare { get; set; }
    public double LongestFlowStreakSeconds { get; set; }
    public double AvgCme { get; set; }
    public double MaxCme { get; set; }
    public double CmeSession { get; set; }
    public List<FlowStatePeriodDto> FlowPeriods { get; set; } = new();
    public List<WindowMetricsDto> WindowDetails { get; set; } = new();
}

public class FlowStatePeriodDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationSeconds { get; set; }
    public double AvgCme { get; set; }
    public double AvgPFlow { get; set; }
}

public class WindowMetricsDto
{
    public DateTime Timestamp { get; set; }
    public double Cme { get; set; }
    public double PFlow { get; set; }
    public bool IsFlow { get; set; }
}

