using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Represents a long-running metaheuristic optimization job for model training.
/// In the real system, this would be a genetic algorithm or particle swarm optimization
/// that repeatedly calls the quantum backend to evaluate candidate models.
/// </summary>
public class TrainingJob
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? ExperimentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = TrainingJobStatus.Queued;

    /// <summary>
    /// Metaheuristic algorithm used for optimization.
    /// </summary>
    [MaxLength(50)]
    public string Algorithm { get; set; } = "genetic";

    /// <summary>
    /// Total number of generations to run.
    /// </summary>
    public int TotalGenerations { get; set; }

    /// <summary>
    /// Number of generations completed so far.
    /// </summary>
    public int CompletedGenerations { get; set; }

    /// <summary>
    /// Best fitness value found (higher is better).
    /// </summary>
    public double? BestFitness { get; set; }

    /// <summary>
    /// Total number of QPU calls made during training.
    /// </summary>
    public int TotalQpuCalls { get; set; }

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Best quantum circuit parameters found during training (JSON array of 8 floats).
    /// Format: [α₀, β₀, α₁, β₁, α₂, β₂, α₃, β₃]
    /// These are used during inference for predictions.
    /// </summary>
    public string? BestParameters { get; set; }

    /// <summary>
    /// Whether this is the currently active model for inference.
    /// </summary>
    public bool IsActiveModel { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(ExperimentId))]
    public virtual Experiment? Experiment { get; set; }
}

public static class TrainingJobStatus
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}


