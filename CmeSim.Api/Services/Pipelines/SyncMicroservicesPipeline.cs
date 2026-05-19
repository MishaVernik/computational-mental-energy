using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace CmeSim.Api.Services.Pipelines;

/// <summary>
/// Architecture B: Synchronous Microservices pipeline - API calls PreprocessService via HTTP, then QPU, then DB.
/// </summary>
public class SyncMicroservicesPipeline : IInferencePipeline
{
    private readonly IDbContextFactory<CmeSimDbContext> _dbContextFactory;
    private readonly IQuantumBackendClient _quantumClient;
    private readonly ICmeCalculator _cmeCalculator;
    private readonly HttpClient _preprocessClient;
    private readonly ILogger<SyncMicroservicesPipeline> _logger;

    public SyncMicroservicesPipeline(
        IDbContextFactory<CmeSimDbContext> dbContextFactory,
        IQuantumBackendClient quantumClient,
        ICmeCalculator cmeCalculator,
        IHttpClientFactory httpClientFactory,
        ILogger<SyncMicroservicesPipeline> logger)
    {
        _dbContextFactory = dbContextFactory;
        _quantumClient = quantumClient;
        _cmeCalculator = cmeCalculator;
        _preprocessClient = httpClientFactory.CreateClient("PreprocessService");
        _logger = logger;
    }

    public async Task<InferenceResponseDto> ExecuteAsync(
        InferenceRequestDto request,
        BenchmarkContext context,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var overallStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.RequestReceived);

        // Stage 1: Validate
        var validateStart = DateTime.UtcNow;
        if (request.Features.Length == 0)
        {
            throw new ArgumentException("Features array cannot be empty");
        }
        if (request.TaskDifficulty < 0 || request.TaskDifficulty > 1)
        {
            throw new ArgumentException("TaskDifficulty must be between 0 and 1");
        }
        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new ArgumentException("Invalid SessionId format");
        }
        var validateMs = (DateTime.UtcNow - validateStart).TotalMilliseconds;
        context.RecordStage("validate", validateMs);
        await context.EmitEventAsync(BenchmarkEventType.Validated, validateMs);

        // Ensure session exists
        var session = await dbContext.Sessions.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session == null)
        {
            session = new Session
            {
                Id = sessionId,
                UserId = "benchmark-user",
                StartedAt = DateTime.UtcNow
            };
            dbContext.Sessions.Add(session);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Load trained parameters
        double[]? trainedParams = null;
        var activeModel = await dbContext.TrainingJobs
            .Where(j => j.IsActiveModel && j.Status == TrainingJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
        {
            try
            {
                trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters);
            }
            catch
            {
                _logger.LogWarning("Failed to deserialize trained parameters, using defaults");
            }
        }

        // Stage 2: Call PreprocessService via HTTP (with network delay simulation)
        var preprocessStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.PreprocessStart);
        
        // Simulate network delay
        if (context.Config.NetworkProfile.MeanMs > 0)
        {
            var networkDelay = SimulateDelay(context.Config.NetworkProfile.MeanMs, context.Config.NetworkProfile.StdMs, context.Config.Seed);
            await Task.Delay(TimeSpan.FromMilliseconds(networkDelay), cancellationToken);
        }
        
        double[] preprocessedFeatures;
        try
        {
            var preprocessRequest = new { features = request.Features };
            var preprocessResponse = await _preprocessClient.PostAsJsonAsync("/api/preprocess", preprocessRequest, cancellationToken);
            preprocessResponse.EnsureSuccessStatusCode();
            var preprocessResult = await preprocessResponse.Content.ReadFromJsonAsync<PreprocessResponse>(cancellationToken: cancellationToken);
            preprocessedFeatures = preprocessResult?.Features ?? request.Features;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PreprocessService call failed");
            // Fallback to original features
            preprocessedFeatures = request.Features;
        }
        
        var preprocessMs = (DateTime.UtcNow - preprocessStart).TotalMilliseconds;
        context.RecordStage("preprocess", preprocessMs);
        await context.EmitEventAsync(BenchmarkEventType.PreprocessEnd, preprocessMs);

        // Stage 3: QPU call (with retry logic for transient failures)
        var qpuWaitStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.QpuWaitStart);
        
        var qpuCallStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.QpuCallStart);
        
        QuantumInferenceResult? quantumResult = null;
        var maxRetries = context.Config.MaxRetries > 0 ? context.Config.MaxRetries : 3;
        var backoffMs = context.Config.BackoffMs > 0 ? context.Config.BackoffMs : 500;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                quantumResult = await _quantumClient.InferAsync(preprocessedFeatures, "QSVC", trainedParams);
                break; // Success
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "QPU call attempt {Attempt}/{MaxRetries} failed, retrying in {BackoffMs}ms", 
                    attempt, maxRetries, backoffMs * attempt);
                await Task.Delay(backoffMs * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quantum backend unavailable after {MaxRetries} attempts", maxRetries);
                throw new InvalidOperationException("Quantum backend is unavailable", ex);
            }
        }
        
        if (quantumResult == null)
        {
            throw new InvalidOperationException("Quantum backend failed to return a result");
        }
        
        var qpuCallMs = (DateTime.UtcNow - qpuCallStart).TotalMilliseconds;
        await context.EmitEventAsync(BenchmarkEventType.QpuCallEnd, qpuCallMs);
        
        var qpuWaitMs = (DateTime.UtcNow - qpuWaitStart).TotalMilliseconds;
        context.RecordStage("qpuWait", qpuWaitMs);
        context.RecordStage("qpuService", quantumResult.QpuLatencyMs);
        await context.EmitEventAsync(BenchmarkEventType.QpuWaitEnd, qpuWaitMs);

        // Stage 4: Compute CME (Вн + index)
        var cmeCalcResult = _cmeCalculator.ComputeCme(preprocessedFeatures, quantumResult.PFlow, request.TaskDifficulty);

        // Stage 5: DB Write (with network delay simulation)
        var dbWriteStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.DbWriteStart);
        
        // Simulate DB delay
        if (context.Config.DbProfile.MeanMs > 0)
        {
            var delay = SimulateDelay(context.Config.DbProfile.MeanMs, context.Config.DbProfile.StdMs, context.Config.Seed);
            await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
        }
        
        var requestLog = new InferenceRequestLog
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WindowId = request.WindowId,
            RequestedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow,
            TotalLatencyMs = (int)(DateTime.UtcNow - overallStart).TotalMilliseconds,
            QpuLatencyMs = quantumResult.QpuLatencyMs,
            IsSuccess = true
        };

        var cmeWindowResult = new CmeWindowResult
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WindowId = request.WindowId,
            ComputedAt = DateTime.UtcNow,
            CmeValue = cmeCalcResult.CmeVn,
            PFlow = quantumResult.PFlow,
            ShotsUsed = quantumResult.ShotsUsed,
            Depth = quantumResult.Depth
        };

        dbContext.InferenceRequestLogs.Add(requestLog);
        dbContext.CmeWindowResults.Add(cmeWindowResult);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var dbWriteMs = (DateTime.UtcNow - dbWriteStart).TotalMilliseconds;
        context.RecordStage("dbWrite", dbWriteMs);
        await context.EmitEventAsync(BenchmarkEventType.DbWriteEnd, dbWriteMs);

        var totalMs = (DateTime.UtcNow - overallStart).TotalMilliseconds;
        context.RecordStage("response", totalMs);
        await context.EmitEventAsync(BenchmarkEventType.ResponseSent, totalMs);

        return new InferenceResponseDto
        {
            CmeVn = cmeCalcResult.CmeVn,
            CmeIndex = cmeCalcResult.CmeIndex,
            PFlow = quantumResult.PFlow,
            ShotsUsed = quantumResult.ShotsUsed,
            Depth = quantumResult.Depth,
            QpuLatencyMs = quantumResult.QpuLatencyMs,
            TotalLatencyMs = (int)totalMs
        };
    }

    private double SimulateDelay(double meanMs, double stdMs, int? seed)
    {
        if (seed.HasValue)
        {
            var random = new Random(seed.Value);
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return Math.Max(0, meanMs + stdMs * z);
        }
        else
        {
            var random = new Random();
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return Math.Max(0, meanMs + stdMs * z);
        }
    }
}

public class PreprocessResponse
{
    public double[] Features { get; set; } = Array.Empty<double>();
}

