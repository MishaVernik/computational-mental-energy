namespace CmeSim.Api.Services;

/// <summary>
/// Client interface for communicating with the Python quantum backend service.
/// </summary>
public interface IQuantumBackendClient
{
    Task<QuantumInferenceResult> InferAsync(double[] features, string modelType = "QSVC", double[]? trainedParams = null);
    Task<List<QuantumInferenceResult>> InferBatchAsync(List<(double[] Features, double[]? TrainedParams)> batch, string modelType = "QSVC");
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Result from quantum inference.
/// </summary>
public class QuantumInferenceResult
{
    public double PFlow { get; set; }
    public int ShotsUsed { get; set; }
    public int Depth { get; set; }
    public int QpuLatencyMs { get; set; }
}


