using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Database entity for a benchmark run.
/// </summary>
public class BenchmarkRun
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    public BenchmarkArchitecture Architecture { get; set; }
    
    // Serialized configuration
    public string ConfigJson { get; set; } = string.Empty;
    
    // Status
    public BenchmarkRunStatus Status { get; set; } = BenchmarkRunStatus.Pending;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Aggregated metrics
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
    
    // Stage metrics (means)
    public double ValidateMs { get; set; }
    public double EnqueueMs { get; set; }
    public double PreprocessMs { get; set; }
    public double QpuWaitMs { get; set; }
    public double QpuServiceMs { get; set; }
    public double DbWriteMs { get; set; }
    public double ResponseMs { get; set; }
    
    // Stage metrics (standard deviations)
    public double ValidateStdMs { get; set; }
    public double EnqueueStdMs { get; set; }
    public double PreprocessStdMs { get; set; }
    public double QpuWaitStdMs { get; set; }
    public double QpuServiceStdMs { get; set; }
    public double DbWriteStdMs { get; set; }
    public double ResponseStdMs { get; set; }
    
    // Serialized aggregated metrics (full JSON)
    public string? MetricsJson { get; set; }
    
    // Navigation
    public virtual ICollection<BenchmarkEvent> Events { get; set; } = new List<BenchmarkEvent>();
}

public enum BenchmarkRunStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

