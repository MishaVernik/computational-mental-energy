namespace CmeSim.Api.Services;

/// <summary>
/// Client interface for the classical flow classifier service.
/// </summary>
public interface IFlowClassifierClient
{
    /// <summary>
    /// Classify flow state from EEG features (22 values: 20 band powers + TaskDifficulty + Quality).
    /// </summary>
    Task<(double FlowProbability, bool FlowLabel)> ClassifyAsync(double[] features, CancellationToken ct = default);

    /// <summary>
    /// Check if flow classifier service is healthy.
    /// </summary>
    Task<bool> IsHealthyAsync();
}
