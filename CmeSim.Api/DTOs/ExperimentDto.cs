namespace CmeSim.Api.DTOs;

public class CreateExperimentRequest
{
    public string Name { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public double OnlineArrivalRate { get; set; }
    public int NumberOfClients { get; set; }
    public double TrainingArrivalRate { get; set; }
    public string? Notes { get; set; }
}

public class ExperimentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int DurationSeconds { get; set; }
    public double OnlineArrivalRate { get; set; }
    public int NumberOfClients { get; set; }
    public double TrainingArrivalRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ExperimentMetricsDto
{
    public Guid ExperimentId { get; set; }
    public double TimeWindowMs { get; set; }
    public InferenceMetrics Inference { get; set; } = new();
    public QpuMetrics Qpu { get; set; } = new();
    public TrainingMetrics Training { get; set; } = new();
    public ComparisonMetrics? Comparison { get; set; }
}

public class InferenceMetrics
{
    public int TotalRequests { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    
    public double AvgLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    
    public double P50LatencyMs { get; set; }
    public double P90LatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    
    public double ThroughputReqPerSec { get; set; }
    
    public Dictionary<string, int>? LatencyHistogram { get; set; }
}

public class QpuMetrics
{
    public int TotalQpuCalls { get; set; }
    public double AvgQpuCallDurationMs { get; set; }
    public double MinQpuCallDurationMs { get; set; }
    public double MaxQpuCallDurationMs { get; set; }
    public double TotalQpuBusyMs { get; set; }
    public double QpuUtilization { get; set; }
    
    public int InferenceCalls { get; set; }
    public int TrainingCalls { get; set; }
    public double QpuBusyMsInference { get; set; }
    public double QpuBusyMsTraining { get; set; }
}

public class TrainingMetrics
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int RunningJobs { get; set; }
    public double CompletionRate { get; set; }
    
    public double AvgJobDurationSec { get; set; }
    public double MinJobDurationSec { get; set; }
    public double MaxJobDurationSec { get; set; }
    public double P50JobDurationSec { get; set; }
    public double P90JobDurationSec { get; set; }
    public double P95JobDurationSec { get; set; }
    
    public Dictionary<string, AlgorithmStats>? ByAlgorithm { get; set; }
}

public class AlgorithmStats
{
    public int JobCount { get; set; }
    public double AvgDurationSec { get; set; }
    public double AvgBestFitness { get; set; }
    public int TotalQpuCalls { get; set; }
}

public class ComparisonMetrics
{
    // Petri net model values
    public double ModelAvgLatencyMs { get; set; }
    public double? ModelP95LatencyMs { get; set; }
    public double ModelThroughputReqPerSec { get; set; }
    public double ModelQpuUtilization { get; set; }
    public double? ModelAvgJobDurationSec { get; set; }
    
    // MAPE (Mean Absolute Percentage Error)
    public double MapeLatency { get; set; }
    public double MapeP95Latency { get; set; }
    public double MapeThroughput { get; set; }
    public double MapeQpuUtilization { get; set; }
    public double? MapeJobDuration { get; set; }
    
    // Overall model accuracy
    public double OverallMape { get; set; }
    public string Verdict { get; set; } = string.Empty; // "Excellent", "Good", "Needs Refinement"
}

public class SaveModelMetricsRequest
{
    public double ModelAvgLatencyMs { get; set; }
    public double? ModelP95LatencyMs { get; set; }
    public double ModelThroughputReqPerSec { get; set; }
    public double ModelQpuUtilization { get; set; }
    public double? ModelAvgJobDurationSec { get; set; }
    public string? Notes { get; set; }
}

