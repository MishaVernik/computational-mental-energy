using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Logs each QPU (quantum backend) invocation for utilization analysis.
/// </summary>
public class QpuInvocationLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ExperimentId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime FinishedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Type of invocation: 'Inference' or 'Training'.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Number of shots used.
    /// </summary>
    public int Shots { get; set; }

    [MaxLength(50)]
    public string? BackendName { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ExperimentId))]
    public virtual Experiment? Experiment { get; set; }
}

public static class QpuInvocationType
{
    public const string Inference = "Inference";
    public const string Training = "Training";
}

