using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for experiment management and metrics analysis.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExperimentsController : ControllerBase
{
    private readonly CmeSimDbContext _dbContext;
    private readonly IExperimentMetricsService _metricsService;
    private readonly ILogger<ExperimentsController> _logger;

    public ExperimentsController(
        CmeSimDbContext dbContext,
        IExperimentMetricsService metricsService,
        ILogger<ExperimentsController> logger)
    {
        _dbContext = dbContext;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new experiment.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExperimentDto>> CreateExperiment([FromBody] CreateExperimentRequest request)
    {
        var experiment = new Experiment
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            StartedAt = DateTime.UtcNow,
            DurationSeconds = request.DurationSeconds,
            OnlineArrivalRate = request.OnlineArrivalRate,
            NumberOfClients = request.NumberOfClients,
            TrainingArrivalRate = request.TrainingArrivalRate,
            Status = ExperimentStatus.Running,
            Notes = request.Notes
        };

        _dbContext.Experiments.Add(experiment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Experiment created: {ExpId}, {Name}", experiment.Id, experiment.Name);

        return Ok(MapToDto(experiment));
    }

    /// <summary>
    /// Mark experiment as completed.
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult> CompleteExperiment(Guid id)
    {
        var experiment = await _dbContext.Experiments.FindAsync(id);
        if (experiment == null)
        {
            return NotFound();
        }

        experiment.Status = ExperimentStatus.Completed;
        experiment.FinishedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Get experiment by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExperimentDto>> GetExperiment(Guid id)
    {
        var experiment = await _dbContext.Experiments.FindAsync(id);
        if (experiment == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(experiment));
    }

    /// <summary>
    /// List all experiments.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExperimentDto>>> ListExperiments([FromQuery] int limit = 20)
    {
        var experiments = await _dbContext.Experiments
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(experiments.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get computed metrics for an experiment.
    /// </summary>
    [HttpGet("{id}/metrics")]
    public async Task<ActionResult<ExperimentMetricsDto>> GetMetrics(Guid id)
    {
        try
        {
            var metrics = await _metricsService.ComputeMetricsAsync(id);
            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute metrics for experiment {ExpId}", id);
            return StatusCode(500, "Failed to compute metrics");
        }
    }

    /// <summary>
    /// Save Petri net model metrics for comparison.
    /// </summary>
    [HttpPost("{id}/modelMetrics")]
    public async Task<ActionResult> SaveModelMetrics(Guid id, [FromBody] SaveModelMetricsRequest request)
    {
        var experiment = await _dbContext.Experiments.FindAsync(id);
        if (experiment == null)
        {
            return NotFound();
        }

        // Check if model metrics already exist
        var existing = await _dbContext.ExperimentModelMetrics
            .FirstOrDefaultAsync(m => m.ExperimentId == id);

        if (existing != null)
        {
            // Update existing
            existing.ModelAvgLatencyMs = request.ModelAvgLatencyMs;
            existing.ModelP95LatencyMs = request.ModelP95LatencyMs;
            existing.ModelThroughputReqPerSec = request.ModelThroughputReqPerSec;
            existing.ModelQpuUtilization = request.ModelQpuUtilization;
            existing.ModelAvgJobDurationSec = request.ModelAvgJobDurationSec;
            existing.Notes = request.Notes;
            existing.SavedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            var modelMetrics = new ExperimentModelMetrics
            {
                Id = Guid.NewGuid(),
                ExperimentId = id,
                ModelAvgLatencyMs = request.ModelAvgLatencyMs,
                ModelP95LatencyMs = request.ModelP95LatencyMs,
                ModelThroughputReqPerSec = request.ModelThroughputReqPerSec,
                ModelQpuUtilization = request.ModelQpuUtilization,
                ModelAvgJobDurationSec = request.ModelAvgJobDurationSec,
                SavedAt = DateTime.UtcNow,
                Notes = request.Notes
            };

            _dbContext.ExperimentModelMetrics.Add(modelMetrics);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Model metrics saved for experiment {ExpId}", id);

        return Ok();
    }

    /// <summary>
    /// Get model metrics for an experiment.
    /// </summary>
    [HttpGet("{id}/modelMetrics")]
    public async Task<ActionResult<ExperimentModelMetrics>> GetModelMetrics(Guid id)
    {
        var modelMetrics = await _dbContext.ExperimentModelMetrics
            .FirstOrDefaultAsync(m => m.ExperimentId == id);

        if (modelMetrics == null)
        {
            return NotFound("Model metrics not found. Use POST to save them first.");
        }

        return Ok(modelMetrics);
    }

    /// <summary>
    /// Export experiment metrics as CSV.
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportMetrics(Guid id)
    {
        try
        {
            var metrics = await _metricsService.ComputeMetricsAsync(id);
            var experiment = await _dbContext.Experiments.FindAsync(id);

            var csv = new StringBuilder();
            csv.AppendLine("CME Quantum ML System - Experiment Metrics Export");
            csv.AppendLine($"Experiment ID:,{id}");
            csv.AppendLine($"Experiment Name:,{experiment?.Name}");
            csv.AppendLine($"Started At:,{experiment?.StartedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Finished At:,{experiment?.FinishedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Duration:,{experiment?.DurationSeconds} seconds");
            csv.AppendLine($"Arrival Rate:,{experiment?.OnlineArrivalRate} req/s");
            csv.AppendLine($"Clients:,{experiment?.NumberOfClients}");
            csv.AppendLine();

            csv.AppendLine("ONLINE INFERENCE METRICS");
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Requests,{metrics.Inference.TotalRequests}");
            csv.AppendLine($"Success Count,{metrics.Inference.SuccessCount}");
            csv.AppendLine($"Error Count,{metrics.Inference.ErrorCount}");
            csv.AppendLine($"Error Rate,{metrics.Inference.ErrorRate:P2}");
            csv.AppendLine($"Avg Latency (ms),{metrics.Inference.AvgLatencyMs:F2}");
            csv.AppendLine($"Min Latency (ms),{metrics.Inference.MinLatencyMs:F2}");
            csv.AppendLine($"Max Latency (ms),{metrics.Inference.MaxLatencyMs:F2}");
            csv.AppendLine($"P50 Latency (ms),{metrics.Inference.P50LatencyMs:F2}");
            csv.AppendLine($"P90 Latency (ms),{metrics.Inference.P90LatencyMs:F2}");
            csv.AppendLine($"P95 Latency (ms),{metrics.Inference.P95LatencyMs:F2}");
            csv.AppendLine($"P99 Latency (ms),{metrics.Inference.P99LatencyMs:F2}");
            csv.AppendLine($"Throughput (req/s),{metrics.Inference.ThroughputReqPerSec:F3}");
            csv.AppendLine();

            csv.AppendLine("QPU UTILIZATION METRICS");
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total QPU Calls,{metrics.Qpu.TotalQpuCalls}");
            csv.AppendLine($"Avg Call Duration (ms),{metrics.Qpu.AvgQpuCallDurationMs:F2}");
            csv.AppendLine($"Total Busy Time (ms),{metrics.Qpu.TotalQpuBusyMs:F2}");
            csv.AppendLine($"QPU Utilization,{metrics.Qpu.QpuUtilization:P2}");
            csv.AppendLine($"Inference Calls,{metrics.Qpu.InferenceCalls}");
            csv.AppendLine($"Training Calls,{metrics.Qpu.TrainingCalls}");
            csv.AppendLine();

            csv.AppendLine("TRAINING JOB METRICS");
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Jobs,{metrics.Training.TotalJobs}");
            csv.AppendLine($"Completed Jobs,{metrics.Training.CompletedJobs}");
            csv.AppendLine($"Failed Jobs,{metrics.Training.FailedJobs}");
            csv.AppendLine($"Completion Rate,{metrics.Training.CompletionRate:P2}");
            csv.AppendLine($"Avg Job Duration (s),{metrics.Training.AvgJobDurationSec:F2}");
            csv.AppendLine($"P95 Job Duration (s),{metrics.Training.P95JobDurationSec:F2}");
            csv.AppendLine();

            if (metrics.Comparison != null)
            {
                csv.AppendLine("MODEL vs REAL COMPARISON (MAPE)");
                csv.AppendLine("Metric,Real Value,Model Value,MAPE (%)");
                csv.AppendLine($"Avg Latency,{metrics.Inference.AvgLatencyMs:F2},{metrics.Comparison.ModelAvgLatencyMs:F2},{metrics.Comparison.MapeLatency:P2}");
                csv.AppendLine($"P95 Latency,{metrics.Inference.P95LatencyMs:F2},{metrics.Comparison.ModelP95LatencyMs},{metrics.Comparison.MapeP95Latency:P2}");
                csv.AppendLine($"Throughput,{metrics.Inference.ThroughputReqPerSec:F3},{metrics.Comparison.ModelThroughputReqPerSec:F3},{metrics.Comparison.MapeThroughput:P2}");
                csv.AppendLine($"QPU Utilization,{metrics.Qpu.QpuUtilization:P2},{metrics.Comparison.ModelQpuUtilization:P2},{metrics.Comparison.MapeQpuUtilization:P2}");
                csv.AppendLine();
                csv.AppendLine($"Overall MAPE,{metrics.Comparison.OverallMape:P2}");
                csv.AppendLine($"Verdict,{metrics.Comparison.Verdict}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"experiment_{id}_metrics.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export metrics for experiment {ExpId}", id);
            return StatusCode(500, "Failed to export metrics");
        }
    }

    private static ExperimentDto MapToDto(Experiment exp)
    {
        return new ExperimentDto
        {
            Id = exp.Id,
            Name = exp.Name,
            StartedAt = exp.StartedAt,
            FinishedAt = exp.FinishedAt,
            DurationSeconds = exp.DurationSeconds,
            OnlineArrivalRate = exp.OnlineArrivalRate,
            NumberOfClients = exp.NumberOfClients,
            TrainingArrivalRate = exp.TrainingArrivalRate,
            Status = exp.Status,
            Notes = exp.Notes
        };
    }
}

