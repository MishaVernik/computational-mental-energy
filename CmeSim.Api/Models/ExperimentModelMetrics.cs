using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Stores Petri net / CPN model metrics for comparison with real system.
/// </summary>
public class ExperimentModelMetrics
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Average latency from Petri net simulation (ms).
    /// </summary>
    public double ModelAvgLatencyMs { get; set; }

    /// <summary>
    /// P95 latency from model (ms).
    /// </summary>
    public double? ModelP95LatencyMs { get; set; }

    /// <summary>
    /// Throughput from model (requests per second).
    /// </summary>
    public double ModelThroughputReqPerSec { get; set; }

    /// <summary>
    /// QPU utilization from model (0-1).
    /// </summary>
    public double ModelQpuUtilization { get; set; }

    /// <summary>
    /// Average training job duration from model (seconds).
    /// </summary>
    public double? ModelAvgJobDurationSec { get; set; }

    public DateTime SavedAt { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ExperimentId))]
    public virtual Experiment? Experiment { get; set; }
}

