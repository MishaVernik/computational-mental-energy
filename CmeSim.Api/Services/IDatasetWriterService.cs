namespace CmeSim.Api.Services;

/// <summary>
/// Enqueues EEG window data for async write to FlowDataset.EegWindowFeatures.
/// Non-blocking: fire-and-forget from hot path.
/// </summary>
public interface IDatasetWriterService
{
    /// <summary>
    /// Enqueue a window for async write. Does not block.
    /// </summary>
    void Enqueue(EegWindowWriteRequest request);
}

/// <summary>
/// Request to write EEG window features to FlowDataset.
/// </summary>
public record EegWindowWriteRequest
{
    public required Guid SessionId { get; init; }
    public required string WindowId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required double TaskDifficulty { get; init; }
    public required double Quality { get; init; }
    public required Dictionary<string, ChannelBandPowers> Channels { get; init; }
    public Guid? ActionSpikeId { get; init; }
}

public record ChannelBandPowers(double Delta, double Theta, double Alpha, double Beta, double Gamma);
