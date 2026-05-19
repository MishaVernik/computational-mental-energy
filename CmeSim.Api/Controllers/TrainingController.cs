using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for managing training jobs.
/// 
/// Training job flow:
/// Client → POST /api/training/start
///       → Create TrainingJob (status=Queued) in DB
///       → Background worker (TrainingWorkerService) detects job
///       → Worker runs metaheuristic loop:
///           - For N generations:
///               - Evaluate K candidates
///               - Each candidate calls quantum backend
///               - Update fitness, population
///       → Worker marks job as Completed
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly CmeSimDbContext _dbContext;
    private readonly ILogger<TrainingController> _logger;
    private readonly IConfiguration _configuration;

    public TrainingController(
        CmeSimDbContext dbContext,
        ILogger<TrainingController> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Start a new training job.
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<TrainingJobDto>> StartTrainingJob([FromBody] StartTrainingJobRequestDto? request)
    {
        try
        {
            var defaultGenerations = _configuration.GetValue<int>("TrainingWorker:GenerationsPerJob", 10);
            var totalGenerations = request?.TotalGenerations ?? defaultGenerations;
            var algorithm = request?.Algorithm ?? "genetic";

            // Validate algorithm
            var validAlgorithms = new[] { "genetic", "pso", "aco", "simulated_annealing" };
            if (!validAlgorithms.Contains(algorithm.ToLower()))
            {
                return BadRequest($"Invalid algorithm. Must be one of: {string.Join(", ", validAlgorithms)}");
            }

            var job = new TrainingJob
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = TrainingJobStatus.Queued,
                Algorithm = algorithm.ToLower(),
                TotalGenerations = totalGenerations,
                CompletedGenerations = 0,
                TotalQpuCalls = 0
            };

            _dbContext.TrainingJobs.Add(job);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Training job created: {JobId}, algorithm={Algorithm}, generations={Generations}",
                job.Id, algorithm, totalGenerations);

            return Ok(MapToDto(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create training job");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get training job status.
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<ActionResult<TrainingJobDto>> GetTrainingJob(Guid jobId)
    {
        var job = await _dbContext.TrainingJobs.FindAsync(jobId);

        if (job == null)
        {
            return NotFound($"Training job {jobId} not found");
        }

        return Ok(MapToDto(job));
    }

    /// <summary>
    /// List all training jobs.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TrainingJobDto>>> ListTrainingJobs(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50)
    {
        var query = _dbContext.TrainingJobs.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(jobs.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Promote a specific training job as the active model.
    /// </summary>
    [HttpPost("{jobId}/promote")]
    public async Task<ActionResult<TrainingJobDto>> PromoteModel(Guid jobId)
    {
        var job = await _dbContext.TrainingJobs.FindAsync(jobId);
        if (job == null) return NotFound();
        if (job.Status != TrainingJobStatus.Completed || string.IsNullOrEmpty(job.BestParameters))
            return BadRequest("Job must be completed with trained parameters");

        var activeModels = await _dbContext.TrainingJobs.Where(j => j.IsActiveModel).ToListAsync();
        foreach (var m in activeModels) m.IsActiveModel = false;
        job.IsActiveModel = true;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Promoted model {JobId} (algorithm={Algo}, fitness={Fitness:F3}) as active",
            jobId, job.Algorithm, job.BestFitness);
        return Ok(MapToDto(job));
    }

    private static TrainingJobDto MapToDto(TrainingJob job)
    {
        return new TrainingJobDto
        {
            Id = job.Id,
            Status = job.Status,
            Algorithm = job.Algorithm,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            TotalGenerations = job.TotalGenerations,
            CompletedGenerations = job.CompletedGenerations,
            BestFitness = job.BestFitness,
            TotalQpuCalls = job.TotalQpuCalls,
            ErrorMessage = job.ErrorMessage,
            BestParameters = job.BestParameters,
            IsActiveModel = job.IsActiveModel
        };
    }
}


