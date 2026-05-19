using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CmeSim.Api.Services;

/// <summary>
/// HTTP client for the Python flow classifier service.
/// </summary>
public class FlowClassifierHttpClient : IFlowClassifierClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FlowClassifierHttpClient> _logger;

    public FlowClassifierHttpClient(HttpClient httpClient, ILogger<FlowClassifierHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(double FlowProbability, bool FlowLabel)> ClassifyAsync(double[] features, CancellationToken ct = default)
    {
        try
        {
            var request = new ClassifyRequest { Features = features.ToList() };
            var response = await _httpClient.PostAsJsonAsync("/classify", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClassifyResponse>(cancellationToken: ct);
            if (result == null)
                throw new InvalidOperationException("Flow classifier returned null response");

            return (result.FlowProbability, result.FlowLabel);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with flow classifier");
            throw new InvalidOperationException("Flow classifier is unavailable", ex);
        }
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

    private class ClassifyRequest
    {
        [JsonPropertyName("features")]
        public List<double> Features { get; set; } = new();
    }

    private class ClassifyResponse
    {
        [JsonPropertyName("flow_probability")]
        public double FlowProbability { get; set; }

        [JsonPropertyName("flow_label")]
        public bool FlowLabel { get; set; }
    }
}
