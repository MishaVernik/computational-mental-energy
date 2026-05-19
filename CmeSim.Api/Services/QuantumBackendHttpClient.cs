using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CmeSim.Api.Services;

/// <summary>
/// HTTP client for Python quantum backend service.
/// </summary>
public class QuantumBackendHttpClient : IQuantumBackendClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuantumBackendHttpClient> _logger;

    public QuantumBackendHttpClient(HttpClient httpClient, ILogger<QuantumBackendHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<QuantumInferenceResult> InferAsync(double[] features, string modelType = "QSVC", double[]? trainedParams = null)
    {
        try
        {
            var request = new
            {
                features = features,
                modelType = modelType,
                trainedParams = trainedParams
            };

            if (trainedParams != null)
            {
                _logger.LogInformation("Calling quantum backend: /qpu/infer with {FeatureCount} features and TRAINED parameters", features.Length);
            }
            else
            {
                _logger.LogInformation("Calling quantum backend: /qpu/infer with {FeatureCount} features (default parameters)", features.Length);
            }

            var response = await _httpClient.PostAsJsonAsync("/qpu/infer", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QpuInferResponse>();

            if (result == null)
            {
                throw new InvalidOperationException("Quantum backend returned null response");
            }

            _logger.LogInformation("Quantum inference complete: p_flow={PFlow:F3}, latency={Latency}ms", 
                result.PFlow, result.QpuLatencyMs);

            return new QuantumInferenceResult
            {
                PFlow = result.PFlow,
                ShotsUsed = result.ShotsUsed,
                Depth = result.Depth,
                QpuLatencyMs = result.QpuLatencyMs
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with quantum backend");
            throw new InvalidOperationException("Quantum backend is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quantum inference failed");
            throw;
        }
    }

    public async Task<List<QuantumInferenceResult>> InferBatchAsync(
        List<(double[] Features, double[]? TrainedParams)> batch, string modelType = "QSVC")
    {
        try
        {
            var request = new
            {
                samples = batch.Select(b => new
                {
                    features = b.Features,
                    modelType = modelType,
                    trainedParams = b.TrainedParams
                }).ToArray()
            };

            _logger.LogInformation("Batch inference: {Count} samples", batch.Count);
            var response = await _httpClient.PostAsJsonAsync("/qpu/infer-batch", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<BatchInferResponse>();
            _logger.LogInformation("Batch complete: {Count} results in {Ms}ms", result?.Results?.Count ?? 0, result?.TotalMs ?? 0);

            return result?.Results?.Select(r => new QuantumInferenceResult
            {
                PFlow = r.PFlow,
                ShotsUsed = r.ShotsUsed,
                Depth = r.Depth,
                QpuLatencyMs = r.QpuLatencyMs
            }).ToList() ?? new List<QuantumInferenceResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch inference failed, falling back to sequential");
            var results = new List<QuantumInferenceResult>();
            foreach (var (features, trainedParams) in batch)
                results.Add(await InferAsync(features, modelType, trainedParams));
            return results;
        }
    }

    private class BatchInferResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("results")]
        public List<BatchResultItem>? Results { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("totalMs")]
        public int TotalMs { get; set; }
    }

    private class BatchResultItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("pFlow")]
        public double PFlow { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("shotsUsed")]
        public int ShotsUsed { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("depth")]
        public int Depth { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("qpuLatencyMs")]
        public int QpuLatencyMs { get; set; }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Internal DTOs for JSON deserialization
    private class QpuInferResponse
    {
        [JsonPropertyName("pFlow")]
        public double PFlow { get; set; }

        [JsonPropertyName("shotsUsed")]
        public int ShotsUsed { get; set; }

        [JsonPropertyName("depth")]
        public int Depth { get; set; }

        [JsonPropertyName("qpuLatencyMs")]
        public int QpuLatencyMs { get; set; }
    }
}


