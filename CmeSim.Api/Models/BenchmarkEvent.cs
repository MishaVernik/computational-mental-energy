using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Individual timing event for a benchmark request (for detailed analysis and Petri net mapping).
/// </summary>
public class BenchmarkEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid BenchmarkRunId { get; set; }
    
    [Required]
    public Guid RequestId { get; set; } // Unique per request within the run
    
    public BenchmarkEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public double DurationMs { get; set; } // Duration for this stage (if applicable)
    
    // Additional metadata
    public string? Metadata { get; set; } // JSON for extra context
    
    // Navigation
    [ForeignKey(nameof(BenchmarkRunId))]
    public virtual BenchmarkRun BenchmarkRun { get; set; } = null!;
}

public enum BenchmarkEventType
{
    RequestReceived,
    Validated,
    EnqueuedToWorker,
    WorkerStart,
    PreprocessStart,
    PreprocessEnd,
    QpuWaitStart,
    QpuWaitEnd,
    QpuCallStart,
    QpuCallEnd,
    DbWriteStart,
    DbWriteEnd,
    ResponseSent,
    AckSent, // For brokered mode
    ResultReady // For brokered mode
}

