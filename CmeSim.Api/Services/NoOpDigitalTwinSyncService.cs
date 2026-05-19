using CmeSim.Api.Hubs;

namespace CmeSim.Api.Services;

/// <summary>
/// Registered when <see cref="AzureDigitalTwinsOptions.Endpoint"/> is empty.
/// Makes the rest of the system unaware of whether Azure DT is configured.
/// </summary>
public sealed class NoOpDigitalTwinSyncService : IDigitalTwinSyncService
{
    private readonly ILogger<NoOpDigitalTwinSyncService> _logger;

    public NoOpDigitalTwinSyncService(ILogger<NoOpDigitalTwinSyncService> logger)
    {
        _logger = logger;
        _logger.LogInformation(
            "AzureDigitalTwins:Endpoint is empty - digital twin sync is disabled (local-only mode).");
    }

    public void RecordWindow(EegWindowDto eeg, CmeResultDto cme, string? activitySlug, double complexity, string inferenceMode) { }
    public void SessionStarted(Guid sessionId, string anonymizedUserId, string inferenceMode) { }
    public void SessionEnded(Guid sessionId, SessionFinalDto final) { }
    public void SetActiveActivity(Guid sessionId, string activitySlug, string activityDisplayName) { }
    public Task EnsureBaseTwinsAsync(CancellationToken ct = default) => Task.CompletedTask;
}
