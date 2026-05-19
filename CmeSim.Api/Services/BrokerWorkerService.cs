using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using CmeSim.Api.Services.Pipelines;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Services;

/// <summary>
/// Background worker service that processes items from the broker queue (Architecture C).
/// </summary>
public class BrokerWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBrokerQueue _broker;
    private readonly ILogger<BrokerWorkerService> _logger;
    private readonly Dictionary<Guid, InferenceResponseDto> _results = new();
    private readonly object _resultsLock = new();

    public BrokerWorkerService(
        IServiceProvider serviceProvider,
        IBrokerQueue broker,
        ILogger<BrokerWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BrokerWorkerService started");

        var tasks = new List<Task>();
        for (int i = 0; i < 4; i++) // 4 worker threads
        {
            tasks.Add(ProcessQueueAsync(stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _broker.DequeueAsync(cancellationToken);
                if (workItem == null)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                await ProcessWorkItemAsync(workItem, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing work item");
            }
        }
    }

    private async Task ProcessWorkItemAsync(BrokerWorkItem workItem, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();
        var quantumClient = scope.ServiceProvider.GetRequiredService<IQuantumBackendClient>();
        var cmeCalculator = scope.ServiceProvider.GetRequiredService<ICmeCalculator>();

        var context = workItem.Context;
        await context.EmitEventAsync(BenchmarkEventType.WorkerStart);

        try
        {
            var request = workItem.Request;
            var overallStart = DateTime.UtcNow;

            // Ensure session exists
            if (!Guid.TryParse(request.SessionId, out var sessionId))
            {
                throw new ArgumentException("Invalid SessionId format");
            }

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

            // Preprocess
            var preprocessStart = DateTime.UtcNow;
            await context.EmitEventAsync(BenchmarkEventType.PreprocessStart);
            // Simulate preprocessing
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
            var preprocessMs = (DateTime.UtcNow - preprocessStart).TotalMilliseconds;
            context.RecordStage("preprocess", preprocessMs);
            await context.EmitEventAsync(BenchmarkEventType.PreprocessEnd, preprocessMs);

            // QPU call (with retry logic)
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
                    quantumResult = await quantumClient.InferAsync(request.Features, "QSVC", trainedParams);
                    break; // Success
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Worker QPU call attempt {Attempt}/{MaxRetries} failed, retrying in {BackoffMs}ms", 
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

            // Compute CME (Вн + index)
            var cmeCalcResult = cmeCalculator.ComputeCme(request.Features, quantumResult.PFlow, request.TaskDifficulty);

            // DB Write
            var dbWriteStart = DateTime.UtcNow;
            await context.EmitEventAsync(BenchmarkEventType.DbWriteStart);
            
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

            var result = new InferenceResponseDto
            {
                CmeVn = cmeCalcResult.CmeVn,
                CmeIndex = cmeCalcResult.CmeIndex,
                PFlow = quantumResult.PFlow,
                ShotsUsed = quantumResult.ShotsUsed,
                Depth = quantumResult.Depth,
                QpuLatencyMs = quantumResult.QpuLatencyMs,
                TotalLatencyMs = (int)(DateTime.UtcNow - overallStart).TotalMilliseconds
            };

            // Store result for polling
            lock (_resultsLock)
            {
                _results[workItem.RequestId] = result;
            }

            await context.EmitEventAsync(BenchmarkEventType.ResultReady);
            
            // Signal completion to the waiting pipeline (end-to-end benchmark)
            workItem.CompletionSource?.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process work item {RequestId}", workItem.RequestId);
            
            // Signal failure to the waiting pipeline
            workItem.CompletionSource?.TrySetException(ex);
            
            throw;
        }
    }

    public InferenceResponseDto? GetResult(Guid requestId)
    {
        lock (_resultsLock)
        {
            return _results.TryGetValue(requestId, out var result) ? result : null;
        }
    }
}

