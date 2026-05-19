using CmeSim.Api.Hubs;
using CmeSim.Api.Services;
using Xunit;

namespace CmeSim.Api.Tests;

/// <summary>
/// Sanity checks for the per-window derivations: orderings (not exact magnitudes)
/// because formulas are ratios that depend on the relative band power profile.
/// </summary>
public class DerivedMetricsServiceTests
{
    private const string UserId = "test-user";
    private const double Budget = 1000.0;

    private static EegWindowDto MakeWindow(double delta, double theta, double alpha, double beta, double gamma)
    {
        var bp = new BandPowersDto { Delta = delta, Theta = theta, Alpha = alpha, Beta = beta, Gamma = gamma };
        return new EegWindowDto
        {
            Timestamp = DateTime.UtcNow,
            Channels = new Dictionary<string, BandPowersDto>
            {
                ["TP9"]  = bp,
                ["AF7"]  = bp,
                ["AF8"]  = bp,
                ["TP10"] = bp
            },
            ChannelQuality = new Dictionary<string, double> {
                ["TP9"] = 0.95, ["AF7"] = 0.95, ["AF8"] = 0.95, ["TP10"] = 0.95
            },
            Quality = 0.95,
            Touching = true,
            SourceMode = "live"
        };
    }

    private static CmeResultDto MakeCme(double cmeVn, double pFlow, bool isFlow) =>
        new() { CmeVn = cmeVn, CmeSessionVn = cmeVn, PFlow = pFlow, IsFlow = isFlow, TotalWindows = 1 };

    [Fact]
    public void EyesOpen_BetaHeavy_Has_HighestEngagement()
    {
        var svc = new DerivedMetricsService();
        var eyesOpen = MakeWindow(delta: 4, theta: 4, alpha: 4, beta: 20, gamma: 3);
        var eyesClosed = MakeWindow(delta: 4, theta: 4, alpha: 20, beta: 4, gamma: 3);
        var drowsy = MakeWindow(delta: 4, theta: 20, alpha: 4, beta: 4, gamma: 3);

        var open = svc.Compute("u1", eyesOpen, MakeCme(1, 0.5, false), Budget);
        var closed = svc.Compute("u2", eyesClosed, MakeCme(1, 0.5, false), Budget);
        var drow = svc.Compute("u3", drowsy, MakeCme(1, 0.5, false), Budget);

        Assert.True(open.EngagementIndex > closed.EngagementIndex);
        Assert.True(open.EngagementIndex > drow.EngagementIndex);
    }

    [Fact]
    public void EyesClosed_AlphaHeavy_Has_HighestRelaxation()
    {
        var svc = new DerivedMetricsService();
        var eyesOpen = MakeWindow(delta: 4, theta: 4, alpha: 4, beta: 20, gamma: 3);
        var eyesClosed = MakeWindow(delta: 4, theta: 4, alpha: 20, beta: 4, gamma: 3);
        var drowsy = MakeWindow(delta: 4, theta: 20, alpha: 4, beta: 4, gamma: 3);

        var open = svc.Compute("u1", eyesOpen, MakeCme(1, 0.5, false), Budget);
        var closed = svc.Compute("u2", eyesClosed, MakeCme(1, 0.5, false), Budget);
        var drow = svc.Compute("u3", drowsy, MakeCme(1, 0.5, false), Budget);

        Assert.True(closed.RelaxationIndex > open.RelaxationIndex);
        Assert.True(closed.RelaxationIndex > drow.RelaxationIndex);
    }

    [Fact]
    public void Drowsy_ThetaHeavy_Has_HighestCognitiveLoad()
    {
        var svc = new DerivedMetricsService();
        var eyesOpen = MakeWindow(delta: 4, theta: 4, alpha: 4, beta: 20, gamma: 3);
        var eyesClosed = MakeWindow(delta: 4, theta: 4, alpha: 20, beta: 4, gamma: 3);
        var drowsy = MakeWindow(delta: 4, theta: 20, alpha: 4, beta: 4, gamma: 3);

        var open = svc.Compute("u1", eyesOpen, MakeCme(1, 0.5, false), Budget);
        var closed = svc.Compute("u2", eyesClosed, MakeCme(1, 0.5, false), Budget);
        var drow = svc.Compute("u3", drowsy, MakeCme(1, 0.5, false), Budget);

        Assert.True(drow.CognitiveLoadIndex > open.CognitiveLoadIndex);
        Assert.True(drow.CognitiveLoadIndex > closed.CognitiveLoadIndex);
    }

    [Fact]
    public void BudgetUtilization_Clamps_To_One()
    {
        var svc = new DerivedMetricsService();
        var window = MakeWindow(1, 1, 1, 1, 1);

        var d = svc.Compute(UserId, window, new CmeResultDto { CmeSessionVn = 2000, PFlow = 0.5 }, Budget);
        Assert.Equal(1.0, d.BudgetUtilization);

        var d2 = svc.Compute(UserId, window, new CmeResultDto { CmeSessionVn = 500, PFlow = 0.5 }, Budget);
        Assert.Equal(0.5, d2.BudgetUtilization);
    }

    [Fact]
    public void FlowMinutesToday_Accumulates_Only_On_Flow_Windows()
    {
        var svc = new DerivedMetricsService();
        var w = MakeWindow(1, 1, 1, 1, 1);

        var d1 = svc.Compute("uflow", w, MakeCme(0.1, 0.9, true), Budget);
        var d2 = svc.Compute("uflow", w, MakeCme(0.1, 0.5, false), Budget);
        var d3 = svc.Compute("uflow", w, MakeCme(0.1, 0.95, true), Budget);

        Assert.Equal(5.0 / 60.0, d1.FlowMinutesToday, 3);
        Assert.Equal(5.0 / 60.0, d2.FlowMinutesToday, 3);
        Assert.Equal(2 * 5.0 / 60.0, d3.FlowMinutesToday, 3);
    }

    [Fact]
    public void DropoutCounter_Counts_TouchingTransitions()
    {
        var svc = new DerivedMetricsService();
        const string hb = "headband-test";

        Assert.Equal(0, svc.IncrementDropoutIfTransitioned(hb, touchingNow: true));
        Assert.Equal(0, svc.IncrementDropoutIfTransitioned(hb, touchingNow: true));
        Assert.Equal(1, svc.IncrementDropoutIfTransitioned(hb, touchingNow: false));
        Assert.Equal(1, svc.IncrementDropoutIfTransitioned(hb, touchingNow: false));
        Assert.Equal(1, svc.IncrementDropoutIfTransitioned(hb, touchingNow: true));
        Assert.Equal(2, svc.IncrementDropoutIfTransitioned(hb, touchingNow: false));
    }

    [Fact]
    public void RollingSignalQuality_Averages_Recent_Mins()
    {
        var svc = new DerivedMetricsService();
        const string hb = "headband-rq";

        svc.RollingSignalQuality(hb, 1.0);
        svc.RollingSignalQuality(hb, 1.0);
        var avg = svc.RollingSignalQuality(hb, 0.4);

        Assert.InRange(avg, 0.7, 0.95);
    }

    [Fact]
    public void AlphaAsymmetry_Zero_When_Symmetric()
    {
        var svc = new DerivedMetricsService();
        var w = MakeWindow(1, 1, 5, 1, 1);

        var d = svc.Compute("usym", w, MakeCme(0.1, 0.5, false), Budget);
        Assert.Equal(0, d.AlphaAsymmetryIndex);
    }

    [Fact]
    public void AlphaAsymmetry_Positive_When_Right_Dominant()
    {
        var svc = new DerivedMetricsService();
        var bpLeft  = new BandPowersDto { Alpha = 1, Theta = 1, Beta = 1 };
        var bpRight = new BandPowersDto { Alpha = 10, Theta = 1, Beta = 1 };
        var bpCommon = new BandPowersDto { Alpha = 1, Theta = 1, Beta = 1 };
        var w = new EegWindowDto
        {
            Timestamp = DateTime.UtcNow,
            Channels = new Dictionary<string, BandPowersDto>
            {
                ["TP9"] = bpCommon,
                ["AF7"] = bpLeft,
                ["AF8"] = bpRight,
                ["TP10"] = bpCommon
            },
            Touching = true
        };

        var d = svc.Compute("uasym", w, MakeCme(0.1, 0.5, false), Budget);
        Assert.True(d.AlphaAsymmetryIndex > 0);
    }
}
