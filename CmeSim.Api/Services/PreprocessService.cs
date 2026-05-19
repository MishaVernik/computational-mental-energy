namespace CmeSim.Api.Services;

/// <summary>
/// Simple preprocessing service (can be extracted to separate microservice later).
/// Currently just normalizes features.
/// </summary>
public class PreprocessService
{
    private readonly ILogger<PreprocessService> _logger;

    public PreprocessService(ILogger<PreprocessService> logger)
    {
        _logger = logger;
    }

    public double[] Preprocess(double[] features)
    {
        if (features == null || features.Length == 0)
        {
            return Array.Empty<double>();
        }

        // Simple normalization: scale to [0, 1] range
        var max = features.Max(Math.Abs);
        if (max == 0)
        {
            return features;
        }

        return features.Select(f => f / max).ToArray();
    }
}

