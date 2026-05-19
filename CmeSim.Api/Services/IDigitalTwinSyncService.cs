using CmeSim.Api.Hubs;

namespace CmeSim.Api.Services;

/// <summary>
/// Fire-and-forget mirror of the live twin state to Azure Digital Twins.
/// Implementations MUST swallow all exceptions so the local pipeline is never blocked.
/// </summary>
public interface IDigitalTwinSyncService
{
    /// <summary>Called once per processed window. Internally throttled per twin id.</summary>
    void RecordWindow(EegWindowDto eeg, CmeResultDto cme, string? activitySlug, double complexity, string inferenceMode);

    /// <summary>Mark a new session as active. Creates/updates the Session twin.</summary>
    void SessionStarted(Guid sessionId, string anonymizedUserId, string inferenceMode);

    /// <summary>
    /// Mark the active session as ended; patches the final aggregates and updates
    /// User--practiced-->Activity relationship counters with the session's contribution.
    /// </summary>
    void SessionEnded(Guid sessionId, SessionFinalDto final);

    /// <summary>
    /// Replace the Session--hasActivity-->Activity relationship to point at the given slug.
    /// Called whenever the active action changes during a session.
    /// </summary>
    void SetActiveActivity(Guid sessionId, string activitySlug, string activityDisplayName);

    /// <summary>
    /// Ensures the User, Headband, 4 Electrode and Activity-catalogue twins exist (idempotent).
    /// Called once at startup by <see cref="DigitalTwinBootstrapper"/>.
    /// </summary>
    Task EnsureBaseTwinsAsync(CancellationToken ct = default);
}
