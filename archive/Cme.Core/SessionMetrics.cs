namespace Cme.Core;

/// <summary>
/// Metrics computed for a single session.
/// </summary>
public class SessionMetrics
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TotalWindows { get; set; }
    public int FlowWindows { get; set; }
    public double FlowShare { get; set; }
    public double TotalDurationSeconds { get; set; }
    public double FlowDurationSeconds { get; set; }
    public double LongestFlowStreakSeconds { get; set; }
    public double AvgCme { get; set; }
    public double MaxCme { get; set; }
    public double CmeSession { get; set; }
    public List<FlowStatePeriod> FlowPeriods { get; set; } = new();
    public List<WindowMetrics> WindowDetails { get; set; } = new();
}

/// <summary>
/// Represents a period of flow state.
/// </summary>
public class FlowStatePeriod
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationSeconds { get; set; }
    public double AvgCme { get; set; }
    public double AvgPFlow { get; set; }
}

/// <summary>
/// Metrics for a single window.
/// </summary>
public class WindowMetrics
{
    public DateTime Timestamp { get; set; }
    public double Cme { get; set; }
    public double PFlow { get; set; }
    public bool IsFlow { get; set; }
}

/// <summary>
/// Global summary metrics across all sessions.
/// </summary>
public class GlobalMetrics
{
    public int TotalSessions { get; set; }
    public double MeanCmeSession { get; set; }
    public double MedianCmeSession { get; set; }
    public double MeanFlowShare { get; set; }
    public int SessionsFlowShareGe05 { get; set; }
    public int SessionsFlowShareGe07 { get; set; }
    
    // Configuration used
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

