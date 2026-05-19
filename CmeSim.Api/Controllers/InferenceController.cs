using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for online inference (CME computation).
/// 
/// Request flow:
/// Client → POST /api/inference/cme
///       → IQuantumBackendClient.InferAsync() [calls Python service]
///       → ICmeCalculator.ComputeCme()
///       → Store in DB (InferenceRequestLog, CmeWindowResult)
///       → Return response
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InferenceController : ControllerBase
{
    private readonly CmeSimDbContext _dbContext;
    private readonly IQuantumBackendClient _quantumClient;
    private readonly ICmeCalculator _cmeCalculator;
    private readonly ILogger<InferenceController> _logger;

    public InferenceController(
        CmeSimDbContext dbContext,
        IQuantumBackendClient quantumClient,
        ICmeCalculator cmeCalculator,
        ILogger<InferenceController> logger)
    {
        _dbContext = dbContext;
        _quantumClient = quantumClient;
        _cmeCalculator = cmeCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Compute CME for an EEG time window (online inference).
    /// </summary>
    [HttpPost("cme")]
    public async Task<ActionResult<InferenceResponseDto>> ComputeCme([FromBody] InferenceRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("CME inference request: session={SessionId}, window={WindowId}, features={Count}",
                request.SessionId, request.WindowId, request.Features.Length);

            // Validate request
            if (request.Features.Length == 0)
            {
                return BadRequest("Features array cannot be empty");
            }

            if (request.TaskDifficulty < 0 || request.TaskDifficulty > 1)
            {
                return BadRequest("TaskDifficulty must be between 0 and 1");
            }

            // Parse session ID
            if (!Guid.TryParse(request.SessionId, out var sessionId))
            {
                return BadRequest("Invalid SessionId format");
            }

            // Ensure session exists (create if it doesn't)
            var session = await _dbContext.Sessions.FindAsync(new object[] { sessionId });
            if (session == null)
            {
                session = new Session
                {
                    Id = sessionId,
                    UserId = "load-test-user",
                    StartedAt = DateTime.UtcNow
                };
                _dbContext.Sessions.Add(session);
                await _dbContext.SaveChangesAsync();
            }

            // Load active trained model parameters (if available)
            double[]? trainedParams = null;
            var activeModel = await _dbContext.TrainingJobs
                .Where(j => j.IsActiveModel && j.Status == TrainingJobStatus.Completed)
                .OrderByDescending(j => j.CompletedAt)
                .FirstOrDefaultAsync();
            
            if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
            {
                try
                {
                    trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters);
                    _logger.LogInformation("Using trained model from job {JobId}, fitness={Fitness:F3}", 
                        activeModel.Id, activeModel.BestFitness);
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize trained parameters, using defaults");
                }
            }
            else
            {
                _logger.LogInformation("No trained model available, using default parameters");
            }
            
            // Call quantum backend and log QPU invocation
            QuantumInferenceResult quantumResult;
            var qpuStartTime = DateTime.UtcNow;
            try
            {
                quantumResult = await _quantumClient.InferAsync(request.Features, "QSVC", trainedParams);
                
                // Log QPU invocation
                var qpuLog = new QpuInvocationLog
                {
                    Id = Guid.NewGuid(),
                    StartedAt = qpuStartTime,
                    FinishedAt = DateTime.UtcNow,
                    DurationMs = quantumResult.QpuLatencyMs,
                    Type = QpuInvocationType.Inference,
                    Shots = quantumResult.ShotsUsed,
                    BackendName = "qiskit_aer",
                    ExperimentId = null // Will be set if running as part of experiment
                };
                _dbContext.QpuInvocationLogs.Add(qpuLog);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Quantum backend unavailable");
                return StatusCode(503, "Quantum backend is unavailable");
            }

            // Compute CME (returns CmeVn in Вн units + CmeIndex as dimensionless [0..100])
            var cmeResult = _cmeCalculator.ComputeCme(
                request.Features,
                quantumResult.PFlow,
                request.TaskDifficulty);

            stopwatch.Stop();
            var totalLatencyMs = (int)stopwatch.ElapsedMilliseconds;

            // Store results in database
            var requestLog = new InferenceRequestLog
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                WindowId = request.WindowId,
                RequestedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                TotalLatencyMs = totalLatencyMs,
                QpuLatencyMs = quantumResult.QpuLatencyMs,
                IsSuccess = true,
                ExperimentId = null
            };

            var cmeWindowResult = new CmeWindowResult
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                WindowId = request.WindowId,
                ComputedAt = DateTime.UtcNow,
                CmeValue = cmeResult.CmeVn,
                PFlow = quantumResult.PFlow,
                ShotsUsed = quantumResult.ShotsUsed,
                Depth = quantumResult.Depth
            };

            _dbContext.InferenceRequestLogs.Add(requestLog);
            _dbContext.CmeWindowResults.Add(cmeWindowResult);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "CME computed: session={SessionId}, window={WindowId}, cmeVn={CmeVn:F4}, cmeIndex={CmeIndex:F2}, p_flow={PFlow:F3}, latency={Latency}ms",
                request.SessionId, request.WindowId, cmeResult.CmeVn, cmeResult.CmeIndex, quantumResult.PFlow, totalLatencyMs);

            return Ok(new InferenceResponseDto
            {
                CmeVn = cmeResult.CmeVn,
                CmeIndex = cmeResult.CmeIndex,
                PFlow = quantumResult.PFlow,
                ShotsUsed = quantumResult.ShotsUsed,
                Depth = quantumResult.Depth,
                QpuLatencyMs = quantumResult.QpuLatencyMs,
                TotalLatencyMs = totalLatencyMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CME inference failed");
            return StatusCode(500, "Internal server error");
        }
    }
}


