namespace CmeSim.Api.DTOs;

/// <summary>
/// Dashboard summary with aggregated metrics.
/// </summary>
public class DashboardSummaryDto
{
    public int TotalInferenceRequests { get; set; }
    public double AverageCme { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double P95ResponseTimeMs { get; set; }
    public double P99ResponseTimeMs { get; set; }
    public Dictionary<string, int> TrainingJobsByStatus { get; set; } = new();
    public int TotalSessions { get; set; }
}


