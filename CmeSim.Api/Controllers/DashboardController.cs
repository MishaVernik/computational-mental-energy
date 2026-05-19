using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for dashboard metrics and aggregated statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly CmeSimDbContext _dbContext;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(CmeSimDbContext dbContext, ILogger<DashboardController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get aggregated system metrics.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        try
        {
            var totalRequests = await _dbContext.InferenceRequestLogs.CountAsync();
            var averageCme = await _dbContext.CmeWindowResults.AverageAsync(c => (double?)c.CmeValue) ?? 0.0;
            var totalSessions = await _dbContext.Sessions.CountAsync();

            // Calculate latency percentiles
            var latencies = await _dbContext.InferenceRequestLogs
                .OrderBy(l => l.TotalLatencyMs)
                .Select(l => l.TotalLatencyMs)
                .ToListAsync();

            double avgLatency = latencies.Any() ? latencies.Average() : 0.0;
            double p95Latency = CalculatePercentile(latencies, 0.95);
            double p99Latency = CalculatePercentile(latencies, 0.99);

            // Training jobs by status
            var jobsByStatus = await _dbContext.TrainingJobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return Ok(new DashboardSummaryDto
            {
                TotalInferenceRequests = totalRequests,
                AverageCme = Math.Round(averageCme, 2),
                AverageResponseTimeMs = Math.Round(avgLatency, 2),
                P95ResponseTimeMs = Math.Round(p95Latency, 2),
                P99ResponseTimeMs = Math.Round(p99Latency, 2),
                TrainingJobsByStatus = jobsByStatus,
                TotalSessions = totalSessions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard summary");
            return StatusCode(500, "Internal server error");
        }
    }

    private static double CalculatePercentile(List<int> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0.0;

        int index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));

        return sortedValues[index];
    }
}


