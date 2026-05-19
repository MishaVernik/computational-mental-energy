using CmeSim.Api.DTOs;
using CmeSim.Api.Services;
using CmeSim.Api.Data;
using CmeSim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for benchmark runs and results.
/// </summary>
[ApiController]
[Route("api/benchmarks")]
public class BenchmarkController : ControllerBase
{
    private readonly IBenchmarkRunnerService _benchmarkService;
    private readonly CmeSimDbContext _dbContext;
    private readonly ILogger<BenchmarkController> _logger;

    public BenchmarkController(
        IBenchmarkRunnerService benchmarkService,
        CmeSimDbContext dbContext,
        ILogger<BenchmarkController> logger)
    {
        _benchmarkService = benchmarkService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Start a new benchmark run.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<Guid>> RunBenchmark([FromBody] JsonElement payload)
    {
        try
        {
            // Accept both shapes: { ...config fields... } and { "config": { ... } }
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());

            BenchmarkScenarioConfigDto? configDto = null;
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("config", out var configProp))
            {
                configDto = configProp.Deserialize<BenchmarkScenarioConfigDto>(options);
            }
            else
            {
                configDto = payload.Deserialize<BenchmarkScenarioConfigDto>(options);
            }

            if (configDto == null)
            {
                return BadRequest("Invalid benchmark config payload.");
            }

            var runId = await _benchmarkService.StartBenchmarkAsync(configDto);
            return Ok(runId);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Invalid benchmark config payload.");
            return BadRequest("Invalid benchmark config payload.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start benchmark");
            return StatusCode(500, "Failed to start benchmark");
        }
    }

    /// <summary>
    /// Get benchmark run status and results.
    /// </summary>
    [HttpGet("{runId}")]
    public async Task<ActionResult<BenchmarkRunResultDto>> GetBenchmark(Guid runId)
    {
        var result = await _benchmarkService.GetBenchmarkResultAsync(runId);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    /// <summary>
    /// Get benchmark history (last N runs).
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<BenchmarkRunResultDto>>> GetHistory([FromQuery] int limit = 50)
    {
        var history = await _benchmarkService.GetBenchmarkHistoryAsync(limit);
        return Ok(history);
    }

    /// <summary>
    /// Export benchmark run results as CSV or JSON.
    /// </summary>
    [HttpGet("{runId}/export")]
    public async Task<IActionResult> ExportBenchmark(Guid runId, [FromQuery] string format = "json")
    {
        var run = await _dbContext.BenchmarkRuns
            .Include(r => r.Events)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null)
        {
            return NotFound();
        }

        if (format.ToLower() == "csv")
        {
            var csv = GenerateCsv(run);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"benchmark_{runId}.csv");
        }
        else
        {
            var json = GenerateJson(run);
            return File(Encoding.UTF8.GetBytes(json), "application/json", $"benchmark_{runId}.json");
        }
    }

    /// <summary>
    /// Get Petri net parameters for a benchmark run.
    /// </summary>
    [HttpGet("{runId}/petri-params")]
    public async Task<ActionResult<PetriNetParamsDto>> GetPetriNetParams(Guid runId)
    {
        var params_ = await _benchmarkService.GetPetriNetParamsAsync(runId);
        if (params_ == null)
        {
            return NotFound();
        }
        return Ok(params_);
    }

    private string GenerateCsv(BenchmarkRun run)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RunId,Name,Architecture,Status,CreatedAt,StartedAt,CompletedAt," +
                     "AvgLatencyMs,P95LatencyMs,P99LatencyMs,ThroughputRps,FailRate,SuccessCount,FailCount," +
                     "AvgQpuQueueLen,MaxQpuQueueLen,AvgBrokerQueueLen,MaxBrokerQueueLen," +
                     "ValidateMs,PreprocessMs,QpuWaitMs,QpuServiceMs,DbWriteMs,ResponseMs");
        
        sb.AppendLine($"{run.Id},{run.Name},{run.Architecture},{run.Status}," +
                      $"{run.CreatedAt:yyyy-MM-dd HH:mm:ss},{run.StartedAt:yyyy-MM-dd HH:mm:ss},{run.CompletedAt:yyyy-MM-dd HH:mm:ss}," +
                      $"{run.AvgLatencyMs},{run.P95LatencyMs},{run.P99LatencyMs},{run.ThroughputRps},{run.FailRate}," +
                      $"{run.SuccessCount},{run.FailCount}," +
                      $"{run.AvgQpuQueueLen},{run.MaxQpuQueueLen},{run.AvgBrokerQueueLen},{run.MaxBrokerQueueLen}," +
                      $"{run.ValidateMs},{run.PreprocessMs},{run.QpuWaitMs},{run.QpuServiceMs},{run.DbWriteMs},{run.ResponseMs}");
        
        return sb.ToString();
    }

    private string GenerateJson(CmeSim.Api.Models.BenchmarkRun run)
    {
        var result = new BenchmarkRunResultDto
        {
            RunId = run.Id,
            Name = run.Name,
            Architecture = run.Architecture,
            Status = run.Status,
            CreatedAt = run.CreatedAt,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            AvgLatencyMs = run.AvgLatencyMs,
            P95LatencyMs = run.P95LatencyMs,
            P99LatencyMs = run.P99LatencyMs,
            ThroughputRps = run.ThroughputRps,
            FailRate = run.FailRate,
            SuccessCount = run.SuccessCount,
            FailCount = run.FailCount,
            AvgQpuQueueLen = run.AvgQpuQueueLen,
            MaxQpuQueueLen = run.MaxQpuQueueLen,
            AvgBrokerQueueLen = run.AvgBrokerQueueLen,
            MaxBrokerQueueLen = run.MaxBrokerQueueLen,
            StageMetrics = new StageMetricsDto
            {
                ValidateMs = run.ValidateMs,
                EnqueueMs = run.EnqueueMs,
                PreprocessMs = run.PreprocessMs,
                QpuWaitMs = run.QpuWaitMs,
                QpuServiceMs = run.QpuServiceMs,
                DbWriteMs = run.DbWriteMs,
                ResponseMs = run.ResponseMs,
                ValidateStdMs = run.ValidateStdMs,
                EnqueueStdMs = run.EnqueueStdMs,
                PreprocessStdMs = run.PreprocessStdMs,
                QpuWaitStdMs = run.QpuWaitStdMs,
                QpuServiceStdMs = run.QpuServiceStdMs,
                DbWriteStdMs = run.DbWriteStdMs,
                ResponseStdMs = run.ResponseStdMs
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(new
        {
            run = result,
            config = run.ConfigJson,
            metrics = run.MetricsJson
        }, options);
    }
}

