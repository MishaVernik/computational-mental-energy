using CmeSim.Api.Hubs;

namespace CmeSim.Api.Services;

/// <summary>
/// Per-user/headband stateful derivations layered on top of the stateless CmeCalculator.
/// Lives as a singleton so rolling buffers and day-counters survive across windows.
/// Pure computation: all exceptions are caller's responsibility (none are expected).
/// </summary>
public interface IDerivedMetricsService
{
    /// <summary>
    /// Computes the user-level derived indices for the current window.
    /// Mutates internal per-user state (flowMinutesToday, fatigueLevel theta ring).
    /// </summary>
    DerivedMetrics Compute(string userId, EegWindowDto eeg, CmeResultDto cme, double cmeBudgetVn);

    /// <summary>Returns the new last-hour transition count if touching changed; otherwise the current count.</summary>
    int IncrementDropoutIfTransitioned(string headbandId, bool touchingNow);

    /// <summary>Updates the rolling 60s mean of min(channelQuality) and returns the current mean.</summary>
    double RollingSignalQuality(string headbandId, double currentMin);
}

/// <summary>
/// Output of <see cref="IDerivedMetricsService.Compute"/>. All fields are deterministic
/// functions of the inputs except <see cref="FlowMinutesToday"/> and <see cref="FatigueLevel"/>
/// which depend on accumulated per-user state.
/// </summary>
public sealed record DerivedMetrics(
    double EngagementIndex,
    double CognitiveLoadIndex,
    double RelaxationIndex,
    double AlphaAsymmetryIndex,
    double FlowMinutesToday,
    double BudgetUtilization,
    double FatigueLevel);
