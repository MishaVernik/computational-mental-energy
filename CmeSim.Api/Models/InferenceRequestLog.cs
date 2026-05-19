using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Logs each online inference request for performance analysis.
/// </summary>
public class InferenceRequestLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string WindowId { get; set; } = string.Empty;

    public Guid? ExperimentId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Total latency from client request to response (milliseconds).
    /// </summary>
    public int TotalLatencyMs { get; set; }

    /// <summary>
    /// Time spent in quantum backend (milliseconds).
    /// </summary>
    public int QpuLatencyMs { get; set; }

    /// <summary>
    /// Whether the request succeeded.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error type if failed.
    /// </summary>
    [MaxLength(100)]
    public string? ErrorType { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SessionId))]
    public virtual Session? Session { get; set; }

    [ForeignKey(nameof(ExperimentId))]
    public virtual Experiment? Experiment { get; set; }
}


