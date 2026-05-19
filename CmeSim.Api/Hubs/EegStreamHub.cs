using System.Collections.Concurrent;
using CmeSim.Api.Data;
using CmeSim.Api.Models;
using CmeSim.Api.Models.FlowDataset;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Hubs;

/// <summary>
/// Calibration state: collects baseline windows to compute κ and feature normalization stats.
/// </summary>
public class CalibrationState
{
    public Guid ActionSpikeId { get; set; }
    public string ActionSlug { get; set; }
    public string ActionName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<double> CmeRateRawValues { get; } = new();
    public double[] FeatureMin { get; set; } = Enumerable.Repeat(double.MaxValue, 8).ToArray();
    public double[] FeatureMax { get; set; } = Enumerable.Repeat(double.MinValue, 8).ToArray();
    public int CleanWindowsCollected { get; set; }
    public int WindowsNeeded { get; set; } = 24;
    public bool IsComplete { get; set; }
    public double CalibratedKappa { get; set; }

    public void AddWindow(double[] features, double cmeRateRaw)
    {
        CmeRateRawValues.Add(cmeRateRaw);
        for (int i = 0; i < Math.Min(features.Length, 8); i++)
        {
            if (features[i] < FeatureMin[i]) FeatureMin[i] = features[i];
            if (features[i] > FeatureMax[i]) FeatureMax[i] = features[i];
        }
        CleanWindowsCollected++;
    }

    public void Complete(double calibrationTarget)
    {
        var maxRate = CmeRateRawValues.Count > 0 ? CmeRateRawValues.Max() : 1.0;
        CalibratedKappa = maxRate > 1e-6 ? calibrationTarget / maxRate : 1.0;
        IsComplete = true;
    }

    public CalibrationContext ToContext() => new(CalibratedKappa, FeatureMin, FeatureMax);
}

/// <summary>
/// Real-time SignalR hub for streaming EEG data from Muse Athena,
/// processing it through the quantum backend, computing CME,
/// and broadcasting results to all connected dashboard clients.
/// </summary>
public class EegStreamHub : Hub
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EegStreamHub> _logger;
    private readonly IDatasetWriterService _datasetWriter;
    private readonly IDigitalTwinSyncService _twinSync;
    private readonly IDerivedMetricsService _derived;

    private static readonly Dictionary<string, Guid> _connectionSessions = new();
    private static readonly Dictionary<string, List<double>> _sessionCmeValues = new();
    private static readonly Dictionary<string, string> _connectionModes = new();

    // Per-action calibration: keyed by (ConnectionId, actionSlug)
    private static readonly ConcurrentDictionary<(string ConnId, string Slug), CalibrationState> _actionCalibration = new();
    private static readonly ConcurrentDictionary<(string ConnId, string Slug), CalibrationContext> _actionCalibrationResults = new();

    // Active actions per connection
    private static readonly ConcurrentDictionary<string, ActiveAction> _connectionActions = new();

    // Per-session aggregate state for the StopSession summary patch
    private static readonly ConcurrentDictionary<Guid, SessionAggregateState> _sessionAggregates = new();

    public EegStreamHub(
        IServiceProvider serviceProvider,
        ILogger<EegStreamHub> logger,
        IDatasetWriterService datasetWriter,
        IDigitalTwinSyncService twinSync,
        IDerivedMetricsService derived)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _datasetWriter = datasetWriter;
        _twinSync = twinSync;
        _derived = derived;
    }

    private const double CmeBudgetVn = 7_618_000.0;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("Connected", new { connectionId = Context.ConnectionId });
        var activeSession = _connectionSessions.Values.FirstOrDefault();
        if (activeSession != Guid.Empty)
        {
            await Clients.Caller.SendAsync("SessionStarted", new { sessionId = activeSession.ToString() });
        }
        // Notify of active action if any
        if (_connectionActions.TryGetValue(Context.ConnectionId, out var action))
        {
            await Clients.Caller.SendAsync("ActionStarted", new
            {
                actionDefId = action.ActionDefId,
                name = action.Name,
                slug = action.Slug,
                difficulty = action.Difficulty,
                startedAt = action.StartedAt
            });
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        var connId = Context.ConnectionId;

        if (_connectionSessions.TryGetValue(connId, out var droppedSessionId))
        {
            var final = BuildSessionFinal(droppedSessionId, "deviceDisconnect");
            try { _twinSync.SessionEnded(droppedSessionId, final); }
            catch (Exception ex) { _logger.LogWarning(ex, "DT SessionEnded (disconnect) failed"); }
        }

        _connectionSessions.Remove(connId);
        _sessionCmeValues.Remove(connId);
        _connectionModes.Remove(connId);
        _connectionActions.TryRemove(connId, out _);

        // Clean up calibration state for this connection
        foreach (var key in _actionCalibration.Keys.Where(k => k.ConnId == connId).ToList())
            _actionCalibration.TryRemove(key, out _);
        foreach (var key in _actionCalibrationResults.Keys.Where(k => k.ConnId == connId).ToList())
            _actionCalibrationResults.TryRemove(key, out _);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartSession(string? userId)
    {
        var connId = Context.ConnectionId;
        var sessionId = Guid.NewGuid();
        _connectionSessions[connId] = sessionId;
        _sessionCmeValues[connId] = new List<double>();

        // Clear all calibration state for this connection (fresh session = fresh calibration)
        foreach (var key in _actionCalibration.Keys.Where(k => k.ConnId == connId).ToList())
            _actionCalibration.TryRemove(key, out _);
        foreach (var key in _actionCalibrationResults.Keys.Where(k => k.ConnId == connId).ToList())
            _actionCalibrationResults.TryRemove(key, out _);

        // Start default calibration
        _actionCalibration[(connId, "_default")] = new CalibrationState { WindowsNeeded = 24 };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var session = new Session
        {
            Id = sessionId,
            UserId = userId ?? "muse-athena-user",
            StartedAt = DateTime.UtcNow
        };
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        _sessionAggregates[sessionId] = new SessionAggregateState { UserId = session.UserId };

        _logger.LogInformation("Session started: {SessionId} for connection {ConnectionId}", sessionId, connId);
        await Clients.All.SendAsync("SessionStarted", new { sessionId = sessionId.ToString() });
        _twinSync.SessionStarted(sessionId, session.UserId, _connectionModes.GetValueOrDefault(connId, "quantum"));
    }

    public async Task StopSession(string? sessionIdParam = null)
    {
        Guid sessionId;
        if (!string.IsNullOrEmpty(sessionIdParam) && Guid.TryParse(sessionIdParam, out var parsed))
        {
            sessionId = parsed;
            foreach (var kv in _connectionSessions.Where(x => x.Value == sessionId).ToList())
            {
                _connectionSessions.Remove(kv.Key);
                _sessionCmeValues.Remove(kv.Key);
            }
        }
        else if (_connectionSessions.TryGetValue(Context.ConnectionId, out sessionId))
        {
            _connectionSessions.Remove(Context.ConnectionId);
            _sessionCmeValues.Remove(Context.ConnectionId);
        }
        else
        {
            await Clients.Caller.SendAsync("SessionEnded", new { sessionId = (string?)null, message = "No active session" });
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var session = await dbContext.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            session.EndedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var final = BuildSessionFinal(sessionId, "userStop");
        _logger.LogInformation("Session stopped: {SessionId}", sessionId);
        await Clients.All.SendAsync("SessionEnded", new { sessionId = sessionId.ToString(), message = "Session stopped" });
        _twinSync.SessionEnded(sessionId, final);
    }

    /// <summary>
    /// Drains the per-session aggregate state into a SessionFinalDto.
    /// Safe to call multiple times (returns an empty dto if no state was tracked).
    /// </summary>
    private static SessionFinalDto BuildSessionFinal(Guid sessionId, string endedReason)
    {
        if (!_sessionAggregates.TryRemove(sessionId, out var agg))
        {
            return new SessionFinalDto { EndedReason = endedReason };
        }

        double integrity = agg.TotalWindows > 0 ? (double)agg.CleanWindows / agg.TotalWindows : 0;
        double flowMinutes = agg.FlowWindows * 5.0 / 60.0;
        string? best = agg.ByActivity.Count == 0
            ? null
            : agg.ByActivity
                .OrderByDescending(kv => kv.Value.TotalMinutes > 0
                    ? kv.Value.TotalCmeVn / kv.Value.TotalMinutes
                    : kv.Value.TotalCmeVn)
                .First().Key;

        return new SessionFinalDto
        {
            PeakPFlow = Math.Round(agg.PeakPFlow, 4),
            FlowMinutes = Math.Round(flowMinutes, 4),
            DataIntegrityScore = Math.Round(integrity, 4),
            BestActivity = best,
            EndedReason = endedReason,
            ActivityUsage = agg.ByActivity.Values.ToList()
        };
    }

    /// <summary>
    /// Start an action: creates ActionSpike in DB, stores active action, broadcasts.
    /// Resolves session from caller's connection or any active session (bridge may own it).
    /// </summary>
    public async Task StartAction(Guid actionDefinitionId, string? description)
    {
        var connId = Context.ConnectionId;
        if (!_connectionSessions.TryGetValue(connId, out var sessionId))
        {
            // Dashboard doesn't own the session – the bridge does. Find any active session.
            var activeEntry = _connectionSessions.FirstOrDefault();
            if (activeEntry.Value == Guid.Empty)
            {
                await Clients.Caller.SendAsync("Error", new { message = "No active session – start a session first" });
                return;
            }
            sessionId = activeEntry.Value;
        }

        // Stop any previous action
        if (_connectionActions.ContainsKey(connId))
            await StopAction();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var actionDef = await dbContext.ActionDefinitions.FindAsync(actionDefinitionId);
        if (actionDef == null)
        {
            await Clients.Caller.SendAsync("Error", new { message = $"ActionDefinition {actionDefinitionId} not found" });
            return;
        }

        var spike = new ActionSpike
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            ActionType = actionDef.Slug,
            ActionDefinitionId = actionDefinitionId,
            Description = description
        };
        dbContext.ActionSpikes.Add(spike);
        await dbContext.SaveChangesAsync();

        var active = new ActiveAction(
            spike.Id, actionDefinitionId, actionDef.Name, actionDef.Slug,
            actionDef.DefaultDifficulty, DateTime.UtcNow);
        _connectionActions[connId] = active;

        // Start per-action calibration if we don't already have a completed one
        var calKey = (connId, actionDef.Slug);
        if (!_actionCalibrationResults.ContainsKey(calKey) && !_actionCalibration.ContainsKey(calKey))
        {
            _actionCalibration[calKey] = new CalibrationState { WindowsNeeded = 24 };
        }

        _logger.LogInformation("Action started: {ActionName} ({Slug}) for session {SessionId}", actionDef.Name, actionDef.Slug, sessionId);

        _twinSync.SetActiveActivity(sessionId, actionDef.Slug, actionDef.Name);

        await Clients.All.SendAsync("ActionStarted", new
        {
            actionDefId = actionDefinitionId,
            actionSpikeId = spike.Id,
            name = actionDef.Name,
            slug = actionDef.Slug,
            difficulty = actionDef.DefaultDifficulty,
            startedAt = spike.StartTime
        });
    }

    /// <summary>
    /// Stop the current action: sets EndTime, broadcasts.
    /// </summary>
    public async Task StopAction()
    {
        var connId = Context.ConnectionId;
        if (!_connectionActions.TryRemove(connId, out var active))
        {
            await Clients.Caller.SendAsync("Error", new { message = "No active action" });
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var spike = await dbContext.ActionSpikes.FindAsync(active.SpikeId);
        if (spike != null)
        {
            spike.EndTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Action stopped: {ActionName}", active.Name);
        await Clients.All.SendAsync("ActionStopped", new
        {
            actionSpikeId = active.SpikeId,
            name = active.Name,
            slug = active.Slug,
            stoppedAt = DateTime.UtcNow
        });
    }

    public Task SetInferenceMode(string mode)
    {
        var normalized = mode?.ToLowerInvariant() switch
        {
            "classical" => "classical",
            "quantum" => "quantum",
            "hybrid" => "hybrid",
            _ => "quantum"
        };
        _connectionModes[Context.ConnectionId] = normalized;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Receive an EEG window from the Muse bridge, process it through QPU, compute CME,
    /// and broadcast results to all connected dashboard clients.
    /// </summary>
    public async Task SendEegWindow(EegWindowDto data)
    {
        var startTime = DateTime.UtcNow;
        var connId = Context.ConnectionId;

        await Clients.All.SendAsync("ReceiveRawEeg", data);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var quantumClient = scope.ServiceProvider.GetRequiredService<IQuantumBackendClient>();
            var flowClassifierClient = scope.ServiceProvider.GetRequiredService<IFlowClassifierClient>();
            var cmeCalculator = scope.ServiceProvider.GetRequiredService<ICmeCalculator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

            var mode = _connectionModes.TryGetValue(connId, out var m) ? m : "quantum";

            // 0. Resolve session
            if (!_connectionSessions.TryGetValue(connId, out var sessionId))
            {
                sessionId = Guid.NewGuid();
                _connectionSessions[connId] = sessionId;
                _sessionCmeValues[connId] = new List<double>();
                var exists = await dbContext.Sessions.AnyAsync(s => s.Id == sessionId);
                if (!exists)
                {
                    dbContext.Sessions.Add(new Session
                    {
                        Id = sessionId,
                        UserId = "muse-athena-user",
                        StartedAt = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
                }
                _logger.LogInformation("Auto-created session {SessionId} for connection {ConnectionId}", sessionId, connId);
                _actionCalibration.TryAdd((connId, "_default"), new CalibrationState { WindowsNeeded = 24 });
            }
            if (!_sessionCmeValues.ContainsKey(connId))
                _sessionCmeValues[connId] = new List<double>();

            // Resolve current action (check caller first, then any connection – 
            // the bridge sends windows but the dashboard owns the action)
            ActiveAction? activeAction = null;
            if (!_connectionActions.TryGetValue(connId, out activeAction))
            {
                activeAction = _connectionActions.Values.FirstOrDefault();
            }
            var actionSlug = activeAction?.Slug ?? "_default";
            var taskDifficulty = activeAction?.Difficulty ?? data.TaskDifficulty;

            var windowId = $"w_{data.Timestamp:yyyyMMdd_HHmmss_fff}";
            var channels = data.Channels?.ToDictionary(
                kv => kv.Key,
                kv => new ChannelBandPowers(kv.Value.Delta, kv.Value.Theta, kv.Value.Alpha, kv.Value.Beta, kv.Value.Gamma))
                ?? new Dictionary<string, ChannelBandPowers>();
            _datasetWriter.Enqueue(new EegWindowWriteRequest
            {
                SessionId = sessionId,
                WindowId = windowId,
                Timestamp = data.Timestamp,
                TaskDifficulty = taskDifficulty,
                Quality = data.Quality,
                Channels = channels,
                ActionSpikeId = activeAction?.SpikeId
            });

            // 1. Extract features with channel validation
            var (features, windowClass) = ExtractValidatedFeatures(data);
            var fullFeatures = ExtractFullFeaturesForClassical(data);

            // 2. Resolve calibration context for current action (or _default)
            var calKey = (connId, actionSlug);
            CalibrationContext? calCtx = _actionCalibrationResults.GetValueOrDefault(calKey);
            var calState = _actionCalibration.GetValueOrDefault(calKey);

            // 3. Normalize features
            var normalizedFeatures = NormalizeFeatures(features, calCtx, calState);

            double pFlow;
            int shotsUsed = 0;
            int depth = 0;
            int qpuLatencyMs = 0;
            double? classicalPFlow = null;

            if (mode == "classical")
            {
                var (flowProb, _) = await flowClassifierClient.ClassifyAsync(fullFeatures);
                pFlow = flowProb;
            }
            else if (mode == "hybrid")
            {
                var classicalTask = flowClassifierClient.ClassifyAsync(fullFeatures);
                double[]? trainedParams = null;
                var activeModel = await dbContext.TrainingJobs
                    .Where(j => j.IsActiveModel && j.Status == TrainingJobStatus.Completed)
                    .OrderByDescending(j => j.CompletedAt)
                    .FirstOrDefaultAsync();
                if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
                {
                    try { trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters); } catch { }
                }
                var qpuTask = quantumClient.InferAsync(normalizedFeatures, "QSVC", trainedParams);
                await Task.WhenAll(classicalTask, qpuTask);
                var (classFlowProb, _) = await classicalTask;
                var qpuResult = await qpuTask;
                classicalPFlow = classFlowProb;
                pFlow = qpuResult.PFlow;
                shotsUsed = qpuResult.ShotsUsed;
                depth = qpuResult.Depth;
                qpuLatencyMs = qpuResult.QpuLatencyMs;
            }
            else
            {
                double[]? trainedParams = null;
                var activeModel = await dbContext.TrainingJobs
                    .Where(j => j.IsActiveModel && j.Status == TrainingJobStatus.Completed)
                    .OrderByDescending(j => j.CompletedAt)
                    .FirstOrDefaultAsync();
                if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
                {
                    try { trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters); } catch { }
                }
                var qpuResult = await quantumClient.InferAsync(normalizedFeatures, "QSVC", trainedParams);
                pFlow = qpuResult.PFlow;
                shotsUsed = qpuResult.ShotsUsed;
                depth = qpuResult.Depth;
                qpuLatencyMs = qpuResult.QpuLatencyMs;
            }

            // 5. Compute CME with calibration context
            var cmeResult = cmeCalculator.ComputeCme(features, pFlow, taskDifficulty, calibration: calCtx);

            // 6. Per-action calibration: collect baseline windows
            if (calState != null && !calState.IsComplete && windowClass != "rejected")
            {
                double defaultKappa = 10.0; // from CmeConfig default
                double rawRate = calCtx != null
                    ? (calCtx.Kappa > 1e-12 ? cmeResult.CmeRate / calCtx.Kappa : cmeResult.CmeRate)
                    : (defaultKappa > 1e-12 ? cmeResult.CmeRate / defaultKappa : cmeResult.CmeRate);
                calState.AddWindow(features, rawRate);
                windowClass = "calibrating";

                var calLabel = actionSlug == "_default" ? "" : $" [{activeAction?.Name}]";
                await Clients.All.SendAsync("CalibrationProgress", new
                {
                    windowsCollected = calState.CleanWindowsCollected,
                    windowsNeeded = calState.WindowsNeeded,
                    isComplete = false,
                    actionSlug,
                    actionName = activeAction?.Name ?? ""
                });

                if (calState.CleanWindowsCollected >= calState.WindowsNeeded)
                {
                    calState.Complete(100.0);
                    var newCtx = calState.ToContext();
                    _actionCalibrationResults[calKey] = newCtx;
                    _actionCalibration.TryRemove(calKey, out _);
                    _logger.LogInformation("Calibration complete for '{Slug}': κ={Kappa:F4}", actionSlug, newCtx.Kappa);
                    await Clients.All.SendAsync("CalibrationComplete", new
                    {
                        kappa = newCtx.Kappa,
                        featureMin = newCtx.FeatureMin,
                        featureMax = newCtx.FeatureMax,
                        actionSlug,
                        actionName = activeAction?.Name ?? ""
                    });
                }
            }

            bool isFlow = pFlow >= 0.85;
            double eBand = features.Take(5).Sum(Math.Abs);

            if (!_sessionCmeValues.ContainsKey(connId))
                _sessionCmeValues[connId] = new List<double>();
            _sessionCmeValues[connId].Add(cmeResult.CmeVn);

            var cmeValues = _sessionCmeValues[connId];
            double cmeSessionVn = cmeValues.Sum();
            int totalWindows = cmeValues.Count;
            var totalLatencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Update per-session aggregates (drives StopSession summary + User--practiced relationship)
            var agg = _sessionAggregates.GetOrAdd(sessionId, _ => new SessionAggregateState { UserId = "muse-athena-user" });
            if (pFlow > agg.PeakPFlow) agg.PeakPFlow = pFlow;
            if (isFlow) agg.FlowWindows++;
            if (windowClass == "clean") agg.CleanWindows++;
            agg.TotalWindows++;
            if (activeAction != null)
            {
                if (!agg.ByActivity.TryGetValue(activeAction.Slug, out var usage))
                {
                    usage = new ActivityUsageDto { Slug = activeAction.Slug, DisplayName = activeAction.Name };
                    agg.ByActivity[activeAction.Slug] = usage;
                }
                usage.TotalCmeVn += cmeResult.CmeVn;
                usage.TotalMinutes += 5.0 / 60.0;
                usage.SumPFlow += pFlow;
                usage.WindowCount++;
            }

            // 9. Persist result
            try
            {
                var sessionExists = await dbContext.Sessions.AnyAsync(s => s.Id == sessionId);
                if (!sessionExists)
                {
                    dbContext.Sessions.Add(new Session
                    {
                        Id = sessionId,
                        UserId = "muse-athena-user",
                        StartedAt = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
                }

                var cmeWindowResult = new CmeWindowResult
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    WindowId = windowId,
                    ComputedAt = DateTime.UtcNow,
                    CmeValue = cmeResult.CmeVn,
                    PFlow = pFlow,
                    ShotsUsed = shotsUsed,
                    Depth = depth,
                    ActionSpikeId = activeAction?.SpikeId
                };
                dbContext.CmeWindowResults.Add(cmeWindowResult);
                dbContext.InferenceRequestLogs.Add(new InferenceRequestLog
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    WindowId = windowId,
                    RequestedAt = startTime,
                    FinishedAt = DateTime.UtcNow,
                    TotalLatencyMs = totalLatencyMs,
                    QpuLatencyMs = qpuLatencyMs,
                    IsSuccess = true
                });
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB save failed (broadcasting anyway): {Inner}", ex.InnerException?.Message ?? ex.Message);
            }

            // 10. Build and broadcast result
            var resultDto = new CmeResultDto
            {
                Timestamp = data.Timestamp,
                CmeVn = cmeResult.CmeVn,
                CmeIndex = cmeResult.CmeIndex,
                PFlow = pFlow,
                ClassicalPFlow = classicalPFlow,
                IsFlow = isFlow,
                EBand = eBand,
                ShotsUsed = shotsUsed,
                Depth = depth,
                QpuLatencyMs = qpuLatencyMs,
                TotalLatencyMs = totalLatencyMs,
                CmeSessionVn = cmeSessionVn,
                TotalWindows = totalWindows,
                TaskDifficulty = taskDifficulty,
                Channels = data.Channels,
                WindowClass = windowClass,
                ChannelQuality = data.ChannelQuality,
                ActionName = activeAction?.Name,
                ActionSlug = activeAction?.Slug,
                CurrentSessionId = sessionId.ToString()
            };

            try
            {
                var derived = _derived.Compute(agg.UserId, data, resultDto, CmeBudgetVn);
                resultDto.EngagementIndex     = derived.EngagementIndex;
                resultDto.CognitiveLoadIndex  = derived.CognitiveLoadIndex;
                resultDto.RelaxationIndex     = derived.RelaxationIndex;
                resultDto.AlphaAsymmetryIndex = derived.AlphaAsymmetryIndex;
                resultDto.FlowMinutesToday    = derived.FlowMinutesToday;
                resultDto.BudgetUtilization   = derived.BudgetUtilization;
                resultDto.FatigueLevel        = derived.FatigueLevel;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Derived metrics computation failed; falling back to null fields");
            }

            await Clients.All.SendAsync("ReceiveCmeResult", resultDto);

            _twinSync.RecordWindow(data, resultDto, activeAction?.Slug, taskDifficulty, mode);

            _logger.LogInformation(
                "CME: cmeVn={CmeVn:F4}, index={CmeIndex:F2}, p_flow={PFlow:F3}, flow={IsFlow}, mode={Mode}, action={Action}, latency={Latency}ms",
                cmeResult.CmeVn, cmeResult.CmeIndex, pFlow, isFlow, mode, actionSlug, resultDto.TotalLatencyMs);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error processing EEG window: {Inner}", inner);
            await Clients.Caller.SendAsync("Error", new { message = inner });
        }
    }

    private static double[] ExtractFullFeaturesForClassical(EegWindowDto data)
    {
        static void AddChannel(double[] arr, int offset, BandPowersDto? ch)
        {
            if (ch != null)
            {
                arr[offset] = ch.Delta;
                arr[offset + 1] = ch.Theta;
                arr[offset + 2] = ch.Alpha;
                arr[offset + 3] = ch.Beta;
                arr[offset + 4] = ch.Gamma;
            }
        }
        var features = new double[22];
        if (data.Channels != null)
        {
            AddChannel(features, 0, data.Channels.GetValueOrDefault("TP9"));
            AddChannel(features, 5, data.Channels.GetValueOrDefault("AF7"));
            AddChannel(features, 10, data.Channels.GetValueOrDefault("AF8"));
            AddChannel(features, 15, data.Channels.GetValueOrDefault("TP10"));
        }
        features[20] = data.TaskDifficulty;
        features[21] = data.Quality;
        return features;
    }

    private static (double[] Features, string WindowClass) ExtractValidatedFeatures(EegWindowDto data)
    {
        var defaultFeatures = new double[] { 0.5, 0.3, 0.4, 0.2, 0.1, 0.0, 0.5, 0.5 };
        if (data.Channels == null || data.Channels.Count == 0)
            return (defaultFeatures, "rejected");

        double sumDelta = 0, sumTheta = 0, sumAlpha = 0, sumBeta = 0, sumGamma = 0;
        int validCount = 0;
        bool anyClamped = false;
        string[] channelNames = { "TP9", "AF7", "AF8", "TP10" };

        double af7Alpha = 0, af8Alpha = 0;
        bool hasAf7 = false, hasAf8 = false;

        foreach (var name in channelNames)
        {
            if (!data.Channels.TryGetValue(name, out var ch))
                continue;

            double chQuality = data.ChannelQuality?.GetValueOrDefault(name, 1.0) ?? 1.0;
            if (chQuality <= 0)
                continue;

            if (EegLimits.IsChannelReject(ch.Delta, ch.Theta, ch.Alpha, ch.Beta, ch.Gamma))
                continue;

            double d = EegLimits.Clamp(ch.Delta, EegLimits.Delta.Artifact);
            double t = EegLimits.Clamp(ch.Theta, EegLimits.Theta.Artifact);
            double a = EegLimits.Clamp(ch.Alpha, EegLimits.Alpha.Artifact);
            double b = EegLimits.Clamp(ch.Beta, EegLimits.Beta.Artifact);
            double g = EegLimits.Clamp(ch.Gamma, EegLimits.Gamma.Artifact);

            if (d != ch.Delta || t != ch.Theta || a != ch.Alpha || b != ch.Beta || g != ch.Gamma)
                anyClamped = true;

            sumDelta += d; sumTheta += t; sumAlpha += a; sumBeta += b; sumGamma += g;
            validCount++;

            if (name == "AF7") { af7Alpha = a; hasAf7 = true; }
            if (name == "AF8") { af8Alpha = a; hasAf8 = true; }
        }

        if (validCount < 2)
            return (defaultFeatures, "rejected");

        double avgDelta = sumDelta / validCount;
        double avgTheta = sumTheta / validCount;
        double avgAlpha = sumAlpha / validCount;
        double avgBeta = sumBeta / validCount;
        double avgGamma = sumGamma / validCount;

        double frontalAsym = (hasAf7 && hasAf8) ? af8Alpha - af7Alpha : 0;
        double engagement = avgTheta > 0 ? avgBeta / avgTheta : 0.5;

        var features = new[] { avgDelta, avgTheta, avgAlpha, avgBeta, avgGamma, frontalAsym, engagement, data.TaskDifficulty };

        bool allClean = EegLimits.IsAllClean(avgDelta, avgTheta, avgAlpha, avgBeta, avgGamma);
        string windowClass = allClean && !anyClamped ? "clean" : "artifact";

        return (features, windowClass);
    }

    private static double[] NormalizeFeatures(double[] features, CalibrationContext? calCtx, CalibrationState? calState)
    {
        if (calCtx != null)
        {
            return features.Select((f, i) =>
            {
                double range = calCtx.FeatureMax[i] - calCtx.FeatureMin[i];
                if (range < 1e-12) return 0.0;
                double normed = 2.0 * (f - calCtx.FeatureMin[i]) / range - 1.0;
                return Math.Clamp(normed, -1.0, 1.0);
            }).ToArray();
        }

        if (calState?.IsComplete == true)
        {
            return features.Select((f, i) =>
            {
                double range = calState.FeatureMax[i] - calState.FeatureMin[i];
                if (range < 1e-12) return 0.0;
                double normed = 2.0 * (f - calState.FeatureMin[i]) / range - 1.0;
                return Math.Clamp(normed, -1.0, 1.0);
            }).ToArray();
        }

        double minVal = features.Min();
        double maxVal = features.Max();
        double r = maxVal - minVal;
        if (r < 1e-12)
            return features.Select(_ => 0.0).ToArray();
        return features.Select(f => 2.0 * (f - minVal) / r - 1.0).ToArray();
    }
}

// ─── Records / DTOs ────────────────────────────────────────────

public record ActiveAction(
    Guid SpikeId, Guid ActionDefId, string Name, string Slug, double Difficulty, DateTime StartedAt);

public class EegWindowDto
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, BandPowersDto> Channels { get; set; } = new();
    public Dictionary<string, double>? ChannelQuality { get; set; }
    public double Quality { get; set; } = 1.0;
    public double TaskDifficulty { get; set; } = 0.5;
    public bool Touching { get; set; } = true;
    public string? SourceMode { get; set; }
}

public class BandPowersDto
{
    public double Delta { get; set; }
    public double Theta { get; set; }
    public double Alpha { get; set; }
    public double Beta { get; set; }
    public double Gamma { get; set; }
}

public class CmeResultDto
{
    public DateTime Timestamp { get; set; }
    public double CmeVn { get; set; }
    public double CmeIndex { get; set; }
    public double PFlow { get; set; }
    public double? ClassicalPFlow { get; set; }
    public bool IsFlow { get; set; }
    public double EBand { get; set; }
    public int ShotsUsed { get; set; }
    public int Depth { get; set; }
    public int QpuLatencyMs { get; set; }
    public int TotalLatencyMs { get; set; }
    public double CmeSessionVn { get; set; }
    public int TotalWindows { get; set; }
    public double TaskDifficulty { get; set; }
    public Dictionary<string, BandPowersDto>? Channels { get; set; }
    public string WindowClass { get; set; } = "clean";
    public Dictionary<string, double>? ChannelQuality { get; set; }
    public string? ActionName { get; set; }
    public string? ActionSlug { get; set; }

    // Derived indices (computed by DerivedMetricsService). Null when service is unavailable.
    public double? EngagementIndex { get; set; }
    public double? CognitiveLoadIndex { get; set; }
    public double? RelaxationIndex { get; set; }
    public double? AlphaAsymmetryIndex { get; set; }
    public double? FlowMinutesToday { get; set; }
    public double? BudgetUtilization { get; set; }
    public double? FatigueLevel { get; set; }
    public string? CurrentSessionId { get; set; }
}

/// <summary>
/// Final summary emitted on StopSession. Patched in a single ADT call by the sync service.
/// Per-activity stats feed the User--practiced-->Activity relationship counters.
/// </summary>
public class SessionFinalDto
{
    public double PeakPFlow { get; set; }
    public double FlowMinutes { get; set; }
    public double DataIntegrityScore { get; set; }
    public string? BestActivity { get; set; }
    public string EndedReason { get; set; } = "userStop";
    public List<ActivityUsageDto> ActivityUsage { get; set; } = new();
}

public class ActivityUsageDto
{
    public string Slug { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public double TotalCmeVn { get; set; }
    public double TotalMinutes { get; set; }
    public double SumPFlow { get; set; }
    public int WindowCount { get; set; }
}

/// <summary>
/// In-memory running aggregates for a single session. Built up window-by-window in
/// SendEegWindow, drained on StopSession to produce a SessionFinalDto.
/// </summary>
internal sealed class SessionAggregateState
{
    public double PeakPFlow;
    public int FlowWindows;
    public int CleanWindows;
    public int TotalWindows;
    public string UserId = "";
    public readonly Dictionary<string, ActivityUsageDto> ByActivity = new();
}
