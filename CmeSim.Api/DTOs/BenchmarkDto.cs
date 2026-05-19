using CmeSim.Api.Models;

namespace CmeSim.Api.DTOs;

/// <summary>
/// DTO for benchmark scenario configuration.
/// </summary>
public class BenchmarkScenarioConfigDto
{
    public string Name { get; set; } = string.Empty;
    public BenchmarkArchitecture Architecture { get; set; }
    public int? MatrixSize { get; set; }
    public int ActiveClients { get; set; } = 10;
    public int RequestsPerClient { get; set; } = 30;
    public int? DurationSec { get; set; }
    public int ThinkTimeMs { get; set; } = 100;
    public int WorkersCount { get; set; } = 1;
    public int WorkerNodes { get; set; } = 1;
    public int WorkersPerNode { get; set; } = 1;
    public int MaxConcurrentQpuCalls { get; set; } = 1;
    public int QpuBackends { get; set; } = 1;
    public int Shots { get; set; } = 256;
    public int CircuitDepth { get; set; } = 4;
    public string? DataFilePath { get; set; }
    public int? MaxDatasetRows { get; set; }
    public bool TrainingEnabled { get; set; } = false;
    public double TrainingRatePerMin { get; set; } = 0.0;
    public int MaxRetries { get; set; } = 3;
    public int BackoffMs { get; set; } = 100;
    public NetworkProfileDto NetworkProfile { get; set; } = new();
    public DatabaseProfileDto DbProfile { get; set; } = new();
    public BrokerProfileDto BrokerProfile { get; set; } = new();
    public int? Seed { get; set; }
}

public class NetworkProfileDto
{
    public double MeanMs { get; set; } = 5.0;
    public double StdMs { get; set; } = 2.0;
}

public class DatabaseProfileDto
{
    public double MeanMs { get; set; } = 10.0;
    public double StdMs { get; set; } = 3.0;
}

public class BrokerProfileDto
{
    public double MeanMs { get; set; } = 2.0;
    public double StdMs { get; set; } = 1.0;
    public string Mode { get; set; } = "Exponential";
}

/// <summary>
/// DTO for benchmark run result.
/// </summary>
public class BenchmarkRunResultDto
{
    public Guid RunId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BenchmarkArchitecture Architecture { get; set; }
    public BenchmarkRunStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Overall metrics
    public double AvgLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double ThroughputRps { get; set; }
    public double FailRate { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    
    // Queue metrics
    public double AvgQpuQueueLen { get; set; }
    public int MaxQpuQueueLen { get; set; }
    public double AvgBrokerQueueLen { get; set; }
    public int MaxBrokerQueueLen { get; set; }
    
    // Stage metrics
    public StageMetricsDto StageMetrics { get; set; } = new();
}

public class StageMetricsDto
{
    public double ValidateMs { get; set; }
    public double EnqueueMs { get; set; }
    public double PreprocessMs { get; set; }
    public double QpuWaitMs { get; set; }
    public double QpuServiceMs { get; set; }
    public double DbWriteMs { get; set; }
    public double ResponseMs { get; set; }
    
    public double ValidateStdMs { get; set; }
    public double EnqueueStdMs { get; set; }
    public double PreprocessStdMs { get; set; }
    public double QpuWaitStdMs { get; set; }
    public double QpuServiceStdMs { get; set; }
    public double DbWriteStdMs { get; set; }
    public double ResponseStdMs { get; set; }
}

/// <summary>
/// DTO for Petri net parameters derived from benchmark run.
/// </summary>
public class PetriNetParamsDto
{
    public Guid RunId { get; set; }
    public BenchmarkArchitecture Architecture { get; set; }
    
    // Token counts
    public int Workers { get; set; }
    public int QpuCount { get; set; }
    public int QpuConcurrencyGate { get; set; }
    
    // Transition delays (normal distribution parameters)
    public TransitionDelayDto ValidateDelay { get; set; } = new();
    public TransitionDelayDto PreprocessDelay { get; set; } = new();
    public TransitionDelayDto QpuDelay { get; set; } = new();
    public TransitionDelayDto DbWriteDelay { get; set; } = new();
    public TransitionDelayDto NetworkDelay { get; set; } = new();
    public TransitionDelayDto BrokerDelay { get; set; } = new();
    
    // Queue statistics
    public QueueStatsDto QpuQueueStats { get; set; } = new();
    public QueueStatsDto BrokerQueueStats { get; set; } = new();
}

public class TransitionDelayDto
{
    public double MeanMs { get; set; }
    public double StdMs { get; set; }
}

public class QueueStatsDto
{
    public double AvgLength { get; set; }
    public int MaxLength { get; set; }
    public double Utilization { get; set; }
}

