using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using CmeSim.Api.Services.Pipelines;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CmeSim.Api.Services;

/// <summary>
/// Service for running benchmark scenarios and collecting metrics.
/// </summary>
public class BenchmarkRunnerService : IBenchmarkRunnerService
{
    private readonly CmeSimDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BenchmarkRunnerService> _logger;
    private readonly ConcurrentDictionary<Guid, Task> _runningBenchmarks = new();

    public BenchmarkRunnerService(
        CmeSimDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<BenchmarkRunnerService> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Guid> StartBenchmarkAsync(BenchmarkScenarioConfigDto configDto, CancellationToken cancellationToken = default)
    {
        // Convert DTO to model
        var config = new BenchmarkScenarioConfig
        {
            Name = configDto.Name,
            Architecture = configDto.Architecture,
            MatrixSize = configDto.MatrixSize,
            ActiveClients = configDto.ActiveClients,
            RequestsPerClient = configDto.RequestsPerClient,
            DurationSec = configDto.DurationSec,
            ThinkTimeMs = configDto.ThinkTimeMs,
            WorkersCount = configDto.WorkersCount,
            WorkerNodes = configDto.WorkerNodes,
            WorkersPerNode = configDto.WorkersPerNode,
            MaxConcurrentQpuCalls = configDto.MaxConcurrentQpuCalls,
            QpuBackends = configDto.QpuBackends,
            Shots = configDto.Shots,
            CircuitDepth = configDto.CircuitDepth,
            DataFilePath = configDto.DataFilePath,
            MaxDatasetRows = configDto.MaxDatasetRows,
            TrainingEnabled = configDto.TrainingEnabled,
            TrainingRatePerMin = configDto.TrainingRatePerMin,
            MaxRetries = configDto.MaxRetries,
            BackoffMs = configDto.BackoffMs,
            NetworkProfile = new NetworkProfile
            {
                MeanMs = configDto.NetworkProfile.MeanMs,
                StdMs = configDto.NetworkProfile.StdMs
            },
            DbProfile = new DatabaseProfile
            {
                MeanMs = configDto.DbProfile.MeanMs,
                StdMs = configDto.DbProfile.StdMs
            },
            BrokerProfile = new BrokerProfile
            {
                MeanMs = configDto.BrokerProfile.MeanMs,
                StdMs = configDto.BrokerProfile.StdMs,
                Mode = Enum.Parse<BrokerMode>(configDto.BrokerProfile.Mode)
            },
            Seed = configDto.Seed ?? Environment.TickCount
        };

        // Create benchmark run entity
        var run = new BenchmarkRun
        {
            Id = Guid.NewGuid(),
            Name = config.Name,
            Architecture = config.Architecture,
            ConfigJson = JsonSerializer.Serialize(config),
            Status = BenchmarkRunStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.BenchmarkRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Starting benchmark run {RunId} with architecture {Architecture}", run.Id, config.Architecture);

        // Start benchmark in background
        var benchmarkTask = RunBenchmarkAsync(run.Id, config, cancellationToken);
        _runningBenchmarks[run.Id] = benchmarkTask;

        // Don't await - let it run in background
        _ = benchmarkTask.ContinueWith(_ => _runningBenchmarks.TryRemove(run.Id, out _));

        return run.Id;
    }

    private async Task RunBenchmarkAsync(Guid runId, BenchmarkScenarioConfig config, CancellationToken cancellationToken)
    {
        var events = new ConcurrentBag<BenchmarkEvent>();
        var latencies = new ConcurrentBag<double>();
        var datasetFeatures = LoadDatasetFeatures(config);
        var datasetIndex = -1;
        var stageDurations = new Dictionary<string, List<double>>
        {
            ["validate"] = new(),
            ["enqueue"] = new(),
            ["preprocess"] = new(),
            ["qpuWait"] = new(),
            ["qpuService"] = new(),
            ["dbWrite"] = new(),
            ["response"] = new()
        };
        var queueLengths = new ConcurrentBag<int>();
        var brokerQueueLengths = new ConcurrentBag<int>();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();
        var broker = scope.ServiceProvider.GetService<IBrokerQueue>();

        // Get pipeline based on architecture
        IInferencePipeline pipeline = config.Architecture switch
        {
            BenchmarkArchitecture.A_Monolith => scope.ServiceProvider.GetRequiredService<MonolithPipeline>(),
            BenchmarkArchitecture.B_SyncMicroservices => scope.ServiceProvider.GetRequiredService<SyncMicroservicesPipeline>(),
            BenchmarkArchitecture.C_Brokered => scope.ServiceProvider.GetRequiredService<BrokeredPipeline>(),
            _ => throw new NotSupportedException($"Architecture {config.Architecture} not supported")
        };

        // Update status
        var run = await dbContext.BenchmarkRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null) return;

        run.Status = BenchmarkRunStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var startTime = DateTime.UtcNow;
            var endTime = config.DurationSec.HasValue
                ? startTime.AddSeconds(config.DurationSec.Value)
                : DateTime.MaxValue;

            var totalRequests = config.ActiveClients * config.RequestsPerClient;
            var requestCount = 0;
            var successCount = 0;
            var failCount = 0;

            // Event handler
            Task OnEvent(BenchmarkEvent evt)
            {
                // Buffer events; persist after load generation to avoid concurrent DbContext use
                events.Add(evt);
                
                // Track queue lengths
                if (broker != null && (evt.EventType == BenchmarkEventType.EnqueuedToWorker || 
                    evt.EventType == BenchmarkEventType.WorkerStart))
                {
                    queueLengths.Add(broker.GetQueueLength());
                }

                return Task.CompletedTask;
            }

            // Generate load
            var tasks = new List<Task>();
            var random = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();

            for (int clientId = 0; clientId < config.ActiveClients; clientId++)
            {
                int localClientId = clientId;
                tasks.Add(Task.Run(async () =>
                {
                    for (int reqId = 0; reqId < config.RequestsPerClient; reqId++)
                    {
                        if (cancellationToken.IsCancellationRequested || DateTime.UtcNow >= endTime)
                            break;

                        var requestId = Guid.NewGuid();
                        var context = new BenchmarkContext
                        {
                            BenchmarkRunId = runId,
                            RequestId = requestId,
                            Config = config,
                            OnEvent = OnEvent
                        };

                        var request = new InferenceRequestDto
                        {
                            SessionId = Guid.NewGuid().ToString(),
                            WindowId = $"window_{localClientId}_{reqId}",
                            Features = GetFeatures(datasetFeatures, ref datasetIndex, random),
                            TaskDifficulty = random.NextDouble()
                        };

                        var requestStart = DateTime.UtcNow;
                        try
                        {
                            var response = await pipeline.ExecuteAsync(request, context, cancellationToken);
                            var latency = (DateTime.UtcNow - requestStart).TotalMilliseconds;
                            latencies.Add(latency);
                            Interlocked.Increment(ref successCount);

                            // Collect stage durations
                            foreach (var stage in context.StageDurations)
                            {
                                if (stageDurations.ContainsKey(stage.Key))
                                {
                                    stageDurations[stage.Key].Add(stage.Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Request failed in benchmark {RunId}", runId);
                            Interlocked.Increment(ref failCount);
                        }

                        Interlocked.Increment(ref requestCount);

                        // Think time
                        if (config.ThinkTimeMs > 0)
                        {
                            await Task.Delay(config.ThinkTimeMs, cancellationToken);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            // Calculate metrics
            var latencyArray = latencies.ToArray();
            Array.Sort(latencyArray);

            var avgLatency = latencyArray.Length > 0 ? latencyArray.Average() : 0;
            var p95Latency = latencyArray.Length > 0 ? latencyArray[(int)(latencyArray.Length * 0.95)] : 0;
            var p99Latency = latencyArray.Length > 0 ? latencyArray[(int)(latencyArray.Length * 0.99)] : 0;

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var throughput = duration > 0 ? requestCount / duration : 0;
            var failRate = requestCount > 0 ? (double)failCount / requestCount : 0;

            var avgQpuQueueLen = queueLengths.Count > 0 ? queueLengths.Average() : 0;
            var maxQpuQueueLen = queueLengths.Count > 0 ? queueLengths.Max() : 0;
            var avgBrokerQueueLen = brokerQueueLengths.Count > 0 ? brokerQueueLengths.Average() : 0;
            var maxBrokerQueueLen = brokerQueueLengths.Count > 0 ? brokerQueueLengths.Max() : 0;

            // Calculate stage metrics
            var stageMetrics = new Dictionary<string, (double mean, double std)>();
            foreach (var stage in stageDurations)
            {
                if (stage.Value.Count > 0)
                {
                    var mean = stage.Value.Average();
                    var variance = stage.Value.Select(x => Math.Pow(x - mean, 2)).Average();
                    var std = Math.Sqrt(variance);
                    stageMetrics[stage.Key] = (mean, std);
                }
            }

            // Persist buffered events in one batch (single-threaded)
            if (events.Count > 0)
            {
                await dbContext.BenchmarkEvents.AddRangeAsync(events, cancellationToken);
            }

            // Update run with results
            run.Status = BenchmarkRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.AvgLatencyMs = avgLatency;
            run.P95LatencyMs = p95Latency;
            run.P99LatencyMs = p99Latency;
            run.ThroughputRps = throughput;
            run.FailRate = failRate;
            run.SuccessCount = successCount;
            run.FailCount = failCount;
            run.AvgQpuQueueLen = avgQpuQueueLen;
            run.MaxQpuQueueLen = maxQpuQueueLen;
            run.AvgBrokerQueueLen = avgBrokerQueueLen;
            run.MaxBrokerQueueLen = maxBrokerQueueLen;

            if (stageMetrics.ContainsKey("validate"))
            {
                run.ValidateMs = stageMetrics["validate"].mean;
                run.ValidateStdMs = stageMetrics["validate"].std;
            }
            if (stageMetrics.ContainsKey("enqueue"))
            {
                run.EnqueueMs = stageMetrics["enqueue"].mean;
                run.EnqueueStdMs = stageMetrics["enqueue"].std;
            }
            if (stageMetrics.ContainsKey("preprocess"))
            {
                run.PreprocessMs = stageMetrics["preprocess"].mean;
                run.PreprocessStdMs = stageMetrics["preprocess"].std;
            }
            if (stageMetrics.ContainsKey("qpuWait"))
            {
                run.QpuWaitMs = stageMetrics["qpuWait"].mean;
                run.QpuWaitStdMs = stageMetrics["qpuWait"].std;
            }
            if (stageMetrics.ContainsKey("qpuService"))
            {
                run.QpuServiceMs = stageMetrics["qpuService"].mean;
                run.QpuServiceStdMs = stageMetrics["qpuService"].std;
            }
            if (stageMetrics.ContainsKey("dbWrite"))
            {
                run.DbWriteMs = stageMetrics["dbWrite"].mean;
                run.DbWriteStdMs = stageMetrics["dbWrite"].std;
            }
            if (stageMetrics.ContainsKey("response"))
            {
                run.ResponseMs = stageMetrics["response"].mean;
                run.ResponseStdMs = stageMetrics["response"].std;
            }

            // Store full metrics JSON
            var metricsJson = JsonSerializer.Serialize(new
            {
                latencyArray,
                stageDurations,
                queueLengths = queueLengths.ToArray(),
                brokerQueueLengths = brokerQueueLengths.ToArray()
            });
            run.MetricsJson = metricsJson;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Benchmark run {RunId} completed: {SuccessCount} success, {FailCount} failed, avg latency {AvgLatency}ms",
                runId, successCount, failCount, avgLatency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benchmark run {RunId} failed", runId);
            run.Status = BenchmarkRunStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private double[] GenerateRandomFeatures(Random random, int count)
    {
        var features = new double[count];
        for (int i = 0; i < count; i++)
        {
            features[i] = random.NextDouble() * 2 - 1; // Range [-1, 1]
        }
        return features;
    }

    private List<double[]> LoadDatasetFeatures(BenchmarkScenarioConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DataFilePath))
        {
            return new List<double[]>();
        }

        try
        {
            var path = config.DataFilePath;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }
            if (!File.Exists(path))
            {
                _logger.LogWarning("Data file not found at {Path}. Falling back to synthetic features.", path);
                return new List<double[]>();
            }

            var rows = new List<double[]>();
            // Columns kept: Delta_TP9, Delta_AF7, Delta_AF8, Delta_TP10, Theta_TP9, Theta_AF7, Theta_AF8, Theta_TP10
            int[] cols = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var reader = new StreamReader(path);
            // skip header
            reader.ReadLine();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("/muse/elements")) continue; // skip non-data rows
                var parts = line.Split(',');
                if (parts.Length <= cols.Max()) continue;

                var features = new double[cols.Length];
                bool ok = true;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (double.TryParse(parts[cols[i]], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                    {
                        features[i] = val;
                    }
                    else
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    rows.Add(features);
                }

                if (config.MaxDatasetRows.HasValue && rows.Count >= config.MaxDatasetRows.Value)
                {
                    break;
                }
            }

            if (rows.Count == 0)
            {
                _logger.LogWarning("Data file {Path} parsed but no valid feature rows found. Falling back to synthetic features.", path);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dataset features from {Path}", config.DataFilePath);
            return new List<double[]>();
        }
    }

    private double[] GetFeatures(List<double[]> datasetFeatures, ref int datasetIndex, Random random)
    {
        if (datasetFeatures.Count == 0)
        {
            return GenerateRandomFeatures(random, 8);
        }

        var next = Interlocked.Increment(ref datasetIndex);
        var idx = next % datasetFeatures.Count;
        if (idx < 0) idx = 0;
        return datasetFeatures[idx];
    }

    public async Task<BenchmarkRunResultDto?> GetBenchmarkResultAsync(Guid runId)
    {
        var run = await _dbContext.BenchmarkRuns
            .Include(r => r.Events)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return null;

        return new BenchmarkRunResultDto
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
    }

    public async Task<List<BenchmarkRunResultDto>> GetBenchmarkHistoryAsync(int limit = 50)
    {
        var runs = await _dbContext.BenchmarkRuns
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return runs.Select(r => new BenchmarkRunResultDto
        {
            RunId = r.Id,
            Name = r.Name,
            Architecture = r.Architecture,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            AvgLatencyMs = r.AvgLatencyMs,
            P95LatencyMs = r.P95LatencyMs,
            P99LatencyMs = r.P99LatencyMs,
            ThroughputRps = r.ThroughputRps,
            FailRate = r.FailRate,
            SuccessCount = r.SuccessCount,
            FailCount = r.FailCount,
            AvgQpuQueueLen = r.AvgQpuQueueLen,
            MaxQpuQueueLen = r.MaxQpuQueueLen,
            AvgBrokerQueueLen = r.AvgBrokerQueueLen,
            MaxBrokerQueueLen = r.MaxBrokerQueueLen,
            StageMetrics = new StageMetricsDto
            {
                ValidateMs = r.ValidateMs,
                EnqueueMs = r.EnqueueMs,
                PreprocessMs = r.PreprocessMs,
                QpuWaitMs = r.QpuWaitMs,
                QpuServiceMs = r.QpuServiceMs,
                DbWriteMs = r.DbWriteMs,
                ResponseMs = r.ResponseMs,
                ValidateStdMs = r.ValidateStdMs,
                EnqueueStdMs = r.EnqueueStdMs,
                PreprocessStdMs = r.PreprocessStdMs,
                QpuWaitStdMs = r.QpuWaitStdMs,
                QpuServiceStdMs = r.QpuServiceStdMs,
                DbWriteStdMs = r.DbWriteStdMs,
                ResponseStdMs = r.ResponseStdMs
            }
        }).ToList();
    }

    public async Task<PetriNetParamsDto?> GetPetriNetParamsAsync(Guid runId)
    {
        var run = await _dbContext.BenchmarkRuns.FindAsync(new object[] { runId });
        if (run == null) return null;

        var config = JsonSerializer.Deserialize<BenchmarkScenarioConfig>(run.ConfigJson);
        if (config == null) return null;

        return new PetriNetParamsDto
        {
            RunId = run.Id,
            Architecture = run.Architecture,
            Workers = config.WorkersCount,
            QpuCount = config.QpuBackends,
            QpuConcurrencyGate = config.MaxConcurrentQpuCalls,
            ValidateDelay = new TransitionDelayDto { MeanMs = run.ValidateMs, StdMs = run.ValidateStdMs },
            PreprocessDelay = new TransitionDelayDto { MeanMs = run.PreprocessMs, StdMs = run.PreprocessStdMs },
            QpuDelay = new TransitionDelayDto { MeanMs = run.QpuServiceMs, StdMs = run.QpuServiceStdMs },
            DbWriteDelay = new TransitionDelayDto { MeanMs = run.DbWriteMs, StdMs = run.DbWriteStdMs },
            NetworkDelay = new TransitionDelayDto { MeanMs = config.NetworkProfile.MeanMs, StdMs = config.NetworkProfile.StdMs },
            BrokerDelay = new TransitionDelayDto { MeanMs = config.BrokerProfile.MeanMs, StdMs = config.BrokerProfile.StdMs },
            QpuQueueStats = new QueueStatsDto
            {
                AvgLength = run.AvgQpuQueueLen,
                MaxLength = run.MaxQpuQueueLen,
                Utilization = run.AvgQpuQueueLen / Math.Max(1, config.MaxConcurrentQpuCalls)
            },
            BrokerQueueStats = new QueueStatsDto
            {
                AvgLength = run.AvgBrokerQueueLen,
                MaxLength = run.MaxBrokerQueueLen,
                Utilization = run.AvgBrokerQueueLen / Math.Max(1, config.WorkersCount)
            }
        };
    }
}

