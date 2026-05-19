using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Services;

public interface IExperimentMetricsService
{
    Task<ExperimentMetricsDto> ComputeMetricsAsync(Guid experimentId);
    Task<ComparisonMetrics?> ComputeComparisonAsync(Guid experimentId, ExperimentModelMetrics modelMetrics);
}

public class ExperimentMetricsService : IExperimentMetricsService
{
    private readonly CmeSimDbContext _dbContext;
    private readonly ILogger<ExperimentMetricsService> _logger;

    public ExperimentMetricsService(CmeSimDbContext dbContext, ILogger<ExperimentMetricsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ExperimentMetricsDto> ComputeMetricsAsync(Guid experimentId)
    {
        var experiment = await _dbContext.Experiments.FindAsync(experimentId);
        if (experiment == null)
        {
            throw new ArgumentException($"Experiment {experimentId} not found");
        }

        // Compute time window
        var inferenceRequests = await _dbContext.InferenceRequestLogs
            .Where(i => i.ExperimentId == experimentId)
            .ToListAsync();

        var qpuInvocations = await _dbContext.QpuInvocationLogs
            .Where(q => q.ExperimentId == experimentId)
            .ToListAsync();

        var trainingJobs = await _dbContext.TrainingJobs
            .Where(t => t.ExperimentId == experimentId)
            .ToListAsync();

        double timeWindowMs = 0;
        if (inferenceRequests.Any())
        {
            var minTime = inferenceRequests.Min(i => i.RequestedAt);
            var maxTime = inferenceRequests.Max(i => i.FinishedAt ?? i.RequestedAt);
            timeWindowMs = (maxTime - minTime).TotalMilliseconds;
        }
        else if (experiment.FinishedAt.HasValue)
        {
            timeWindowMs = (experiment.FinishedAt.Value - experiment.StartedAt).TotalMilliseconds;
        }

        // Compute inference metrics
        var inferenceMetrics = ComputeInferenceMetrics(inferenceRequests, timeWindowMs);

        // Compute QPU metrics
        var qpuMetrics = ComputeQpuMetrics(qpuInvocations, timeWindowMs);

        // Compute training metrics
        var trainingMetrics = ComputeTrainingMetrics(trainingJobs);

        // Check for model metrics
        var modelMetrics = await _dbContext.ExperimentModelMetrics
            .FirstOrDefaultAsync(m => m.ExperimentId == experimentId);

        ComparisonMetrics? comparison = null;
        if (modelMetrics != null)
        {
            comparison = await ComputeComparisonAsync(experimentId, modelMetrics);
        }

        return new ExperimentMetricsDto
        {
            ExperimentId = experimentId,
            TimeWindowMs = timeWindowMs,
            Inference = inferenceMetrics,
            Qpu = qpuMetrics,
            Training = trainingMetrics,
            Comparison = comparison
        };
    }

    private InferenceMetrics ComputeInferenceMetrics(List<InferenceRequestLog> requests, double timeWindowMs)
    {
        if (!requests.Any())
        {
            return new InferenceMetrics();
        }

        var latencies = requests.Select(r => (double)r.TotalLatencyMs).OrderBy(l => l).ToList();
        var successCount = requests.Count(r => r.IsSuccess);
        var errorCount = requests.Count - successCount;

        return new InferenceMetrics
        {
            TotalRequests = requests.Count,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            ErrorRate = requests.Count > 0 ? (double)errorCount / requests.Count : 0,

            AvgLatencyMs = latencies.Average(),
            MinLatencyMs = latencies.Min(),
            MaxLatencyMs = latencies.Max(),

            P50LatencyMs = Percentile(latencies, 0.50),
            P90LatencyMs = Percentile(latencies, 0.90),
            P95LatencyMs = Percentile(latencies, 0.95),
            P99LatencyMs = Percentile(latencies, 0.99),

            ThroughputReqPerSec = timeWindowMs > 0 ? requests.Count / (timeWindowMs / 1000.0) : 0,

            LatencyHistogram = ComputeLatencyHistogram(latencies)
        };
    }

    private QpuMetrics ComputeQpuMetrics(List<QpuInvocationLog> invocations, double timeWindowMs)
    {
        if (!invocations.Any())
        {
            return new QpuMetrics();
        }

        var durations = invocations.Select(q => (double)q.DurationMs).ToList();
        var inferenceCalls = invocations.Where(q => q.Type == QpuInvocationType.Inference).ToList();
        var trainingCalls = invocations.Where(q => q.Type == QpuInvocationType.Training).ToList();

        var totalBusyMs = invocations.Sum(q => q.DurationMs);
        var utilization = timeWindowMs > 0 ? totalBusyMs / timeWindowMs : 0;

        return new QpuMetrics
        {
            TotalQpuCalls = invocations.Count,
            AvgQpuCallDurationMs = durations.Average(),
            MinQpuCallDurationMs = durations.Min(),
            MaxQpuCallDurationMs = durations.Max(),
            TotalQpuBusyMs = totalBusyMs,
            QpuUtilization = utilization,

            InferenceCalls = inferenceCalls.Count,
            TrainingCalls = trainingCalls.Count,
            QpuBusyMsInference = inferenceCalls.Sum(q => q.DurationMs),
            QpuBusyMsTraining = trainingCalls.Sum(q => q.DurationMs)
        };
    }

    private TrainingMetrics ComputeTrainingMetrics(List<TrainingJob> jobs)
    {
        if (!jobs.Any())
        {
            return new TrainingMetrics();
        }

        var completedJobs = jobs.Where(j => j.Status == TrainingJobStatus.Completed && j.StartedAt.HasValue && j.CompletedAt.HasValue).ToList();
        var durations = completedJobs.Select(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalSeconds).OrderBy(d => d).ToList();

        var byAlgorithm = jobs
            .Where(j => j.Status == TrainingJobStatus.Completed)
            .GroupBy(j => j.Algorithm)
            .ToDictionary(
                g => g.Key,
                g => new AlgorithmStats
                {
                    JobCount = g.Count(),
                    AvgDurationSec = g.Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
                        .Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalSeconds),
                    AvgBestFitness = g.Average(j => j.BestFitness ?? 0),
                    TotalQpuCalls = g.Sum(j => j.TotalQpuCalls)
                });

        return new TrainingMetrics
        {
            TotalJobs = jobs.Count,
            CompletedJobs = jobs.Count(j => j.Status == TrainingJobStatus.Completed),
            FailedJobs = jobs.Count(j => j.Status == TrainingJobStatus.Failed),
            RunningJobs = jobs.Count(j => j.Status == TrainingJobStatus.Running),
            CompletionRate = jobs.Count > 0 ? (double)jobs.Count(j => j.Status == TrainingJobStatus.Completed) / jobs.Count : 0,

            AvgJobDurationSec = durations.Any() ? durations.Average() : 0,
            MinJobDurationSec = durations.Any() ? durations.Min() : 0,
            MaxJobDurationSec = durations.Any() ? durations.Max() : 0,
            P50JobDurationSec = durations.Any() ? Percentile(durations, 0.50) : 0,
            P90JobDurationSec = durations.Any() ? Percentile(durations, 0.90) : 0,
            P95JobDurationSec = durations.Any() ? Percentile(durations, 0.95) : 0,

            ByAlgorithm = byAlgorithm
        };
    }

    public async Task<ComparisonMetrics?> ComputeComparisonAsync(Guid experimentId, ExperimentModelMetrics modelMetrics)
    {
        var realMetrics = await ComputeMetricsAsync(experimentId);

        var mapeLatency = CalculateMape(realMetrics.Inference.AvgLatencyMs, modelMetrics.ModelAvgLatencyMs);
        var mapeP95 = modelMetrics.ModelP95LatencyMs.HasValue
            ? CalculateMape(realMetrics.Inference.P95LatencyMs, modelMetrics.ModelP95LatencyMs.Value)
            : 0;
        var mapeThroughput = CalculateMape(realMetrics.Inference.ThroughputReqPerSec, modelMetrics.ModelThroughputReqPerSec);
        var mapeQpu = CalculateMape(realMetrics.Qpu.QpuUtilization, modelMetrics.ModelQpuUtilization);
        var mapeJobDuration = modelMetrics.ModelAvgJobDurationSec.HasValue && realMetrics.Training.AvgJobDurationSec > 0
            ? CalculateMape(realMetrics.Training.AvgJobDurationSec, modelMetrics.ModelAvgJobDurationSec.Value)
            : (double?)null;

        var mapeValues = new List<double> { mapeLatency, mapeThroughput, mapeQpu };
        if (mapeP95 > 0) mapeValues.Add(mapeP95);
        if (mapeJobDuration.HasValue) mapeValues.Add(mapeJobDuration.Value);

        var overallMape = mapeValues.Average();

        string verdict;
        if (overallMape < 0.10) verdict = "Excellent (<10% error)";
        else if (overallMape < 0.20) verdict = "Good (<20% error)";
        else verdict = "Needs Refinement (>20% error)";

        return new ComparisonMetrics
        {
            ModelAvgLatencyMs = modelMetrics.ModelAvgLatencyMs,
            ModelP95LatencyMs = modelMetrics.ModelP95LatencyMs,
            ModelThroughputReqPerSec = modelMetrics.ModelThroughputReqPerSec,
            ModelQpuUtilization = modelMetrics.ModelQpuUtilization,
            ModelAvgJobDurationSec = modelMetrics.ModelAvgJobDurationSec,

            MapeLatency = mapeLatency,
            MapeP95Latency = mapeP95,
            MapeThroughput = mapeThroughput,
            MapeQpuUtilization = mapeQpu,
            MapeJobDuration = mapeJobDuration,

            OverallMape = overallMape,
            Verdict = verdict
        };
    }

    private static double Percentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;

        int index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));

        return sortedValues[index];
    }

    private static double CalculateMape(double real, double model)
    {
        if (model == 0) return 0;
        return Math.Abs(real - model) / Math.Abs(model);
    }

    private static Dictionary<string, int> ComputeLatencyHistogram(List<double> latencies)
    {
        return new Dictionary<string, int>
        {
            ["<500ms"] = latencies.Count(l => l < 500),
            ["500-1000ms"] = latencies.Count(l => l >= 500 && l < 1000),
            ["1000-1500ms"] = latencies.Count(l => l >= 1000 && l < 1500),
            ["1500-2000ms"] = latencies.Count(l => l >= 1500 && l < 2000),
            [">2000ms"] = latencies.Count(l => l >= 2000)
        };
    }
}

