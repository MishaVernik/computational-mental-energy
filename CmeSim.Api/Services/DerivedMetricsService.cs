using System.Collections.Concurrent;
using CmeSim.Api.Hubs;

namespace CmeSim.Api.Services;

/// <summary>
/// Stateful derivations layered on top of CmeCalculator. Singleton lifetime.
/// All ratios use per-channel band-power means; tiny epsilon guards prevent divide-by-zero
/// when the device is disconnected or a channel is fully clamped.
/// </summary>
public sealed class DerivedMetricsService : IDerivedMetricsService
{
    private const double Eps = 1e-9;
    private const double FlowThreshold = 0.85;
    private const double WindowSeconds = 5.0;
    private const int ThetaRingCapacity = 60;
    private const int QualityRingCapacity = 12;
    private static readonly TimeSpan DropoutWindow = TimeSpan.FromMinutes(60);

    private readonly ConcurrentDictionary<string, UserState> _users = new();
    private readonly ConcurrentDictionary<string, HeadbandState> _headbands = new();

    public DerivedMetrics Compute(string userId, EegWindowDto eeg, CmeResultDto cme, double cmeBudgetVn)
    {
        var state = _users.GetOrAdd(userId, _ => new UserState());
        var today = DateTime.UtcNow.Date;
        if (state.Day != today)
        {
            state.Day = today;
            state.FlowMinutesToday = 0;
        }

        double engagement = 0, cognitiveLoad = 0, relaxation = 0;
        int n = 0;
        double afLeftAlpha = 0, afRightAlpha = 0;
        if (eeg.Channels != null)
        {
            foreach (var (name, bp) in eeg.Channels)
            {
                double alpha = Math.Max(0, bp.Alpha);
                double beta = Math.Max(0, bp.Beta);
                double theta = Math.Max(0, bp.Theta);
                engagement   += beta  / (alpha + theta + Eps);
                cognitiveLoad += theta / (alpha + beta  + Eps);
                relaxation   += alpha / (beta             + Eps);
                n++;
                if (name == "AF7") afLeftAlpha  = alpha;
                if (name == "AF8") afRightAlpha = alpha;
            }
        }
        if (n > 0)
        {
            engagement    /= n;
            cognitiveLoad /= n;
            relaxation    /= n;
        }

        double asymmetry = (afLeftAlpha > Eps && afRightAlpha > Eps)
            ? Math.Log(afRightAlpha) - Math.Log(afLeftAlpha)
            : 0;

        if (cme.IsFlow)
        {
            state.FlowMinutesToday += WindowSeconds / 60.0;
        }

        double budgetUtil = cmeBudgetVn > Eps
            ? Math.Clamp(cme.CmeSessionVn / cmeBudgetVn, 0, 1)
            : 0;

        double thetaMean = 0;
        if (eeg.Channels != null && eeg.Channels.Count > 0)
        {
            thetaMean = eeg.Channels.Values.Average(bp => Math.Max(0, bp.Theta));
        }
        state.ThetaRing.Enqueue(thetaMean);
        while (state.ThetaRing.Count > ThetaRingCapacity) state.ThetaRing.Dequeue();
        double fatigue = ComputeFatigue(state.ThetaRing);

        return new DerivedMetrics(
            EngagementIndex:    Round(engagement),
            CognitiveLoadIndex: Round(cognitiveLoad),
            RelaxationIndex:    Round(relaxation),
            AlphaAsymmetryIndex: Round(asymmetry),
            FlowMinutesToday:   Round(state.FlowMinutesToday),
            BudgetUtilization:  Round(budgetUtil),
            FatigueLevel:       Round(fatigue));
    }

    public int IncrementDropoutIfTransitioned(string headbandId, bool touchingNow)
    {
        var hb = _headbands.GetOrAdd(headbandId, _ => new HeadbandState());
        var now = DateTime.UtcNow;
        lock (hb)
        {
            while (hb.Dropouts.Count > 0 && now - hb.Dropouts.Peek() > DropoutWindow)
            {
                hb.Dropouts.Dequeue();
            }
            if (hb.LastTouching.HasValue && hb.LastTouching.Value && !touchingNow)
            {
                hb.Dropouts.Enqueue(now);
            }
            hb.LastTouching = touchingNow;
            return hb.Dropouts.Count;
        }
    }

    public double RollingSignalQuality(string headbandId, double currentMin)
    {
        var hb = _headbands.GetOrAdd(headbandId, _ => new HeadbandState());
        lock (hb)
        {
            hb.QualityRing.Enqueue(Math.Clamp(currentMin, 0, 1));
            while (hb.QualityRing.Count > QualityRingCapacity) hb.QualityRing.Dequeue();
            return Round(hb.QualityRing.Average());
        }
    }

    private static double ComputeFatigue(Queue<double> thetaRing)
    {
        if (thetaRing.Count < 4) return 0;
        var arr = thetaRing.ToArray();
        int half = arr.Length / 2;
        double earlier = arr.Take(half).Average();
        double later = arr.Skip(half).Average();
        if (earlier < Eps) return 0;
        double ratio = (later - earlier) / earlier;
        return Math.Clamp(ratio, 0, 1);
    }

    private static double Round(double v) => Math.Round(v, 4);

    private sealed class UserState
    {
        public DateTime Day { get; set; } = DateTime.UtcNow.Date;
        public double FlowMinutesToday { get; set; }
        public Queue<double> ThetaRing { get; } = new();
    }

    private sealed class HeadbandState
    {
        public bool? LastTouching { get; set; }
        public Queue<DateTime> Dropouts { get; } = new();
        public Queue<double> QualityRing { get; } = new();
    }
}
