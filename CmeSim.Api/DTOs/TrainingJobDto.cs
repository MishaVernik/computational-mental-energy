namespace CmeSim.Api.DTOs;

/// <summary>
/// Request to start a training job.
/// </summary>
public class StartTrainingJobRequestDto
{
    public int? TotalGenerations { get; set; } // Optional, uses default from config if not provided
    public string? Algorithm { get; set; } // Metaheuristic algorithm: genetic, pso, aco, simulated_annealing
}

/// <summary>
/// Training job status response.
/// </summary>
public class TrainingJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalGenerations { get; set; }
    public int CompletedGenerations { get; set; }
    public double? BestFitness { get; set; }
    public int TotalQpuCalls { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BestParameters { get; set; }
    public bool IsActiveModel { get; set; }
}


