using System.ComponentModel.DataAnnotations;

namespace CmeSim.Api.Models;

/// <summary>
/// Represents a controlled experiment/simulation run for performance analysis and Petri net comparison.
/// </summary>
public class Experiment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Duration in seconds (configured).
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Configured arrival rate for online inference (requests per second).
    /// </summary>
    public double OnlineArrivalRate { get; set; }

    /// <summary>
    /// Number of parallel clients.
    /// </summary>
    public int NumberOfClients { get; set; }

    /// <summary>
    /// Training job arrival rate (jobs per minute).
    /// </summary>
    public double TrainingArrivalRate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = ExperimentStatus.Running;

    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<InferenceRequestLog> InferenceRequests { get; set; } = new List<InferenceRequestLog>();
    public virtual ICollection<TrainingJob> TrainingJobs { get; set; } = new List<TrainingJob>();
    public virtual ICollection<QpuInvocationLog> QpuInvocations { get; set; } = new List<QpuInvocationLog>();
    public virtual ExperimentModelMetrics? ModelMetrics { get; set; }
}

public static class ExperimentStatus
{
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Aborted = "Aborted";
}

