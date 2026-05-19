namespace Cme.Core;

/// <summary>
/// Calculates session and global metrics from EEG window records.
/// </summary>
public class MetricsCalculator
{
    private readonly CmeCalculator _cmeCalculator;
    private readonly CmeConfig _config;

    public MetricsCalculator(CmeConfig? config = null)
    {
        _config = config ?? new CmeConfig();
        _cmeCalculator = new CmeCalculator(_config);
    }

    /// <summary>
    /// Calculate metrics for all sessions.
    /// </summary>
    public (List<SessionMetrics> Sessions, GlobalMetrics Global, double NormalizationFactor) CalculateMetrics(
        List<EegWindowRecord> windows)
    {
        if (windows.Count == 0)
        {
            return (new List<SessionMetrics>(), new GlobalMetrics(), 1.0);
        }

        // Calculate normalization factor
        double k = _cmeCalculator.CalculateNormalizationFactor(windows);

        // Group by session
        var sessions = windows.GroupBy(w => w.SessionId).ToList();

        var sessionMetrics = new List<SessionMetrics>();

        foreach (var sessionGroup in sessions)
        {
            var sessionWindows = sessionGroup.OrderBy(w => w.StartUtc ?? DateTime.MinValue).ToList();
            var metrics = CalculateSessionMetrics(sessionGroup.Key, sessionWindows, k);
            sessionMetrics.Add(metrics);
        }

        // Calculate global metrics
        var globalMetrics = CalculateGlobalMetrics(sessionMetrics, k);

        return (sessionMetrics, globalMetrics, k);
    }

    private SessionMetrics CalculateSessionMetrics(string sessionId, List<EegWindowRecord> windows, double k)
    {
        var metrics = new SessionMetrics
        {
            SessionId = sessionId,
            UserId = windows.FirstOrDefault()?.UserId,
            TotalWindows = windows.Count
        };

        // Calculate CME for each window and create window details
        var windowDetails = new List<WindowMetrics>();
        var cmeValues = new List<double>();
        
        foreach (var window in windows.OrderBy(w => w.StartUtc ?? DateTime.MinValue))
        {
            var cme = _cmeCalculator.CalculateCme(window, k);
            var isFlow = _cmeCalculator.IsFlowWindow(window);
            
            cmeValues.Add(cme);
            windowDetails.Add(new WindowMetrics
            {
                Timestamp = window.StartUtc ?? DateTime.MinValue,
                Cme = cme,
                PFlow = window.FlowProbability,
                IsFlow = isFlow
            });
        }

        metrics.WindowDetails = windowDetails;
        metrics.AvgCme = cmeValues.Any() ? cmeValues.Average() : 0;
        metrics.MaxCme = cmeValues.Any() ? cmeValues.Max() : 0;
        metrics.CmeSession = cmeValues.Sum();

        // Flow detection
        metrics.FlowWindows = windows.Count(w => _cmeCalculator.IsFlowWindow(w));
        metrics.FlowShare = metrics.TotalWindows > 0 
            ? (double)metrics.FlowWindows / metrics.TotalWindows 
            : 0.0;

        // Duration calculations
        metrics.TotalDurationSeconds = windows.Sum(w => w.GetDeltaSeconds());
        metrics.FlowDurationSeconds = windows
            .Where(w => _cmeCalculator.IsFlowWindow(w))
            .Sum(w => w.GetDeltaSeconds());

        // Longest flow streak
        metrics.LongestFlowStreakSeconds = CalculateLongestFlowStreak(windows);

        // Calculate flow periods (when flow started, duration, when stopped)
        metrics.FlowPeriods = CalculateFlowPeriods(windowDetails);

        return metrics;
    }

    private List<FlowStatePeriod> CalculateFlowPeriods(List<WindowMetrics> windows)
    {
        var periods = new List<FlowStatePeriod>();
        if (windows.Count == 0) return periods;

        DateTime? flowStart = null;
        var flowCmes = new List<double>();
        var flowPFlows = new List<double>();

        for (int i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            
            if (window.IsFlow)
            {
                if (flowStart == null)
                {
                    // Flow started
                    flowStart = window.Timestamp;
                    flowCmes.Clear();
                    flowPFlows.Clear();
                }
                
                flowCmes.Add(window.Cme);
                flowPFlows.Add(window.PFlow);
            }
            else
            {
                if (flowStart.HasValue)
                {
                    // Flow ended
                    var duration = (window.Timestamp - flowStart.Value).TotalSeconds;
                    periods.Add(new FlowStatePeriod
                    {
                        StartTime = flowStart.Value,
                        EndTime = window.Timestamp,
                        DurationSeconds = duration,
                        AvgCme = flowCmes.Any() ? flowCmes.Average() : 0,
                        AvgPFlow = flowPFlows.Any() ? flowPFlows.Average() : 0
                    });
                    flowStart = null;
                }
            }
        }

        // Handle case where flow continues to the end
        if (flowStart.HasValue && windows.Any())
        {
            var lastWindow = windows.Last();
            var duration = (lastWindow.Timestamp - flowStart.Value).TotalSeconds;
            periods.Add(new FlowStatePeriod
            {
                StartTime = flowStart.Value,
                EndTime = lastWindow.Timestamp,
                DurationSeconds = duration,
                AvgCme = flowCmes.Any() ? flowCmes.Average() : 0,
                AvgPFlow = flowPFlows.Any() ? flowPFlows.Average() : 0
            });
        }

        return periods;
    }

    private double CalculateLongestFlowStreak(List<EegWindowRecord> windows)
    {
        if (windows.Count == 0) return 0.0;

        double longestStreak = 0.0;
        double currentStreak = 0.0;

        foreach (var window in windows.OrderBy(w => w.StartUtc ?? DateTime.MinValue))
        {
            if (_cmeCalculator.IsFlowWindow(window))
            {
                currentStreak += window.GetDeltaSeconds();
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0.0;
            }
        }

        return longestStreak;
    }

    private GlobalMetrics CalculateGlobalMetrics(List<SessionMetrics> sessions, double k)
    {

        var cmeSessionValues = sessions.Select(s => s.CmeSession).OrderBy(x => x).ToList();
        var flowShareValues = sessions.Select(s => s.FlowShare).ToList();

        return new GlobalMetrics
        {
            TotalSessions = sessions.Count,
            MeanCmeSession = cmeSessionValues.Any() ? cmeSessionValues.Average() : 0.0,
            MedianCmeSession = cmeSessionValues.Any() 
                ? (cmeSessionValues.Count % 2 == 0
                    ? (cmeSessionValues[cmeSessionValues.Count / 2 - 1] + cmeSessionValues[cmeSessionValues.Count / 2]) / 2.0
                    : cmeSessionValues[cmeSessionValues.Count / 2])
                : 0.0,
            MeanFlowShare = flowShareValues.Any() ? flowShareValues.Average() : 0.0,
            SessionsFlowShareGe05 = sessions.Count(s => s.FlowShare >= 0.5),
            SessionsFlowShareGe07 = sessions.Count(s => s.FlowShare >= 0.7),
            K = k,
            WDelta = _config.WDelta,
            WTheta = _config.WTheta,
            WAlpha = _config.WAlpha,
            WBeta = _config.WBeta,
            Lambda1 = _config.Lambda1,
            Lambda2 = _config.Lambda2,
            Lambda3 = _config.Lambda3,
            FlowThreshold = _config.FlowThreshold
        };
    }
}

