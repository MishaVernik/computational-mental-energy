using System.Collections.Concurrent;
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using CmeSim.Api.Hubs;
using Microsoft.Extensions.Options;

namespace CmeSim.Api.Services;

/// <summary>
/// Mirrors the live twin state to Azure Digital Twins as thin summary updates.
///
/// Design constraints:
///   * Fire-and-forget: never throws to callers; all exceptions logged at warning level.
///   * Throttled per twin id by <see cref="AzureDigitalTwinsOptions.SyncIntervalSeconds"/>.
///   * Diff-only updates when enabled, so we only pay for properties that actually changed.
///   * One Activity twin per ActionDefinition (shared catalogue); per-user usage stats
///     live on User--practiced-->Activity relationship properties.
///
/// Cost envelope (West Europe, list prices 2026, 30-s interval):
///   * Single user: ~50-80k ops/month -> ~$0.50-0.80/month.
///   * 500 users at 30-s interval, change-only: ~56M ops -> ~$555/month (anti-pattern;
///     for production scale switch to summary-only updates or use Azure Functions
///     routing IoT Hub messages directly to ADT).
/// </summary>
public sealed class DigitalTwinSyncService : IDigitalTwinSyncService, IAsyncDisposable
{
    private readonly AzureDigitalTwinsOptions _opts;
    private readonly ILogger<DigitalTwinSyncService> _logger;
    private readonly DigitalTwinsClient _client;
    private readonly IDerivedMetricsService _derived;

    private readonly ConcurrentDictionary<string, DateTime> _lastSyncByTwin = new();
    private readonly ConcurrentDictionary<string, IDictionary<string, object>> _lastValuesByTwin = new();

    private Guid? _activeSessionId;
    private string? _activeSessionTwinId;
    private string _activeInferenceMode = "quantum";
    private string? _activeActivity;
    private double _activeComplexity;

    public DigitalTwinSyncService(
        IOptions<AzureDigitalTwinsOptions> opts,
        ILogger<DigitalTwinSyncService> logger,
        IDerivedMetricsService derived)
    {
        _opts = opts.Value;
        _logger = logger;
        _derived = derived;

        var credential = BuildCredential(_opts);
        _client = new DigitalTwinsClient(new Uri(_opts.Endpoint!), credential);

        _logger.LogInformation(
            "DigitalTwinSyncService active: endpoint={Endpoint}, interval={Interval}s, diffOnly={DiffOnly}",
            _opts.Endpoint, _opts.SyncIntervalSeconds, _opts.DiffOnly);
    }

    private static Azure.Core.TokenCredential BuildCredential(AzureDigitalTwinsOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ClientId)
            && !string.IsNullOrWhiteSpace(opts.ClientSecret)
            && !string.IsNullOrWhiteSpace(opts.TenantId))
        {
            return new ClientSecretCredential(opts.TenantId, opts.ClientId, opts.ClientSecret);
        }
        return new DefaultAzureCredential();
    }

    public void RecordWindow(EegWindowDto eeg, CmeResultDto cme, string? activitySlug, double complexity, string inferenceMode)
    {
        _activeActivity = activitySlug;
        _activeComplexity = complexity;
        _activeInferenceMode = inferenceMode;

        // Run the network calls on the thread pool. Any failure is logged and dropped.
        _ = Task.Run(async () =>
        {
            try { await PushUserAndSessionAsync(cme); }
            catch (Exception ex) { _logger.LogWarning(ex, "DT user/session sync failed"); }

            try { await PushHeadbandAsync(eeg); }
            catch (Exception ex) { _logger.LogWarning(ex, "DT headband sync failed"); }

            try { await PushElectrodesAsync(eeg); }
            catch (Exception ex) { _logger.LogWarning(ex, "DT electrode sync failed"); }
        });
    }

    public void SessionStarted(Guid sessionId, string anonymizedUserId, string inferenceMode)
    {
        _activeSessionId = sessionId;
        _activeSessionTwinId = $"session-{sessionId}";
        _activeInferenceMode = inferenceMode;
        _lastValuesByTwin.TryRemove(_activeSessionTwinId, out _);
        _lastSyncByTwin.TryRemove(_activeSessionTwinId, out _);

        _ = Task.Run(async () =>
        {
            try
            {
                await UpsertTwinAsync(_activeSessionTwinId, "dtmi:cme:Session;1", new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId.ToString(),
                    ["startedAt"] = DateTime.UtcNow,
                    ["inferenceMode"] = inferenceMode,
                    ["cumulativeCmeVn"] = 0.0,
                    ["totalWindows"] = 0
                });
                await EnsureRelationshipAsync(_opts.UserTwinId, "runs", _activeSessionTwinId, "rel-user-runs-active");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "DT SessionStarted sync failed"); }
        });
    }

    public void SessionEnded(Guid sessionId, SessionFinalDto final)
    {
        var twinId = _activeSessionTwinId;
        _activeSessionId = null;
        _activeSessionTwinId = null;
        if (twinId == null) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var patch = new Azure.JsonPatchDocument();
                patch.AppendAdd("/endedAt", DateTime.UtcNow);
                patch.AppendAdd("/peakPFlow", final.PeakPFlow);
                patch.AppendAdd("/flowMinutes", final.FlowMinutes);
                patch.AppendAdd("/dataIntegrityScore", final.DataIntegrityScore);
                patch.AppendAdd("/endedReason", final.EndedReason);
                if (!string.IsNullOrEmpty(final.BestActivity))
                {
                    patch.AppendAdd("/bestActivity", final.BestActivity);
                }
                await _client.UpdateDigitalTwinAsync(twinId, patch);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "DT SessionEnded patch failed"); }

            foreach (var usage in final.ActivityUsage)
            {
                try { await BumpUserPracticedAsync(usage); }
                catch (Exception ex) { _logger.LogDebug(ex, "DT practiced bump failed for {Slug}", usage.Slug); }
            }
        });
    }

    public void SetActiveActivity(Guid sessionId, string activitySlug, string activityDisplayName)
    {
        _activeActivity = activitySlug;
        var sessTwinId = _activeSessionTwinId ?? $"session-{sessionId}";
        var activityTwinId = $"activity-{activitySlug}";

        _ = Task.Run(async () =>
        {
            try
            {
                await UpsertTwinAsync(activityTwinId, "dtmi:cme:Activity;1", new Dictionary<string, object>
                {
                    ["slug"] = activitySlug,
                    ["displayName"] = activityDisplayName
                });
                var rel = new BasicRelationship
                {
                    Id = $"rel-sess-{sessionId}-activity",
                    SourceId = sessTwinId,
                    TargetId = activityTwinId,
                    Name = "hasActivity"
                };
                await _client.CreateOrReplaceRelationshipAsync(sessTwinId, rel.Id, rel);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "DT SetActiveActivity failed for slug {Slug}", activitySlug);
            }
        });
    }

    public async Task EnsureBaseTwinsAsync(CancellationToken ct = default)
    {
        try
        {
            await UpsertTwinAsync(_opts.UserTwinId, "dtmi:cme:User;1", new Dictionary<string, object>
            {
                ["displayName"] = "Lab user",
                ["anonymizedUserId"] = _opts.UserTwinId,
                ["cmeBudgetVn"] = 7_618_000.0,
                ["cmeSpentTodayVn"] = 0.0,
                ["lastSeenAt"] = DateTime.UtcNow
            });

            await UpsertTwinAsync(_opts.HeadbandTwinId, "dtmi:cme:Headband;1", new Dictionary<string, object>
            {
                ["model"] = "Muse Athena",
                ["channelCount"] = 4,
                ["samplingRateHz"] = 256,
                ["connected"] = false,
                ["sourceMode"] = "simulator",
                ["connectionState"] = "disconnected",
                ["dropoutCountLastHour"] = 0,
                ["lastSignalQualityMean"] = 0.0
            });

            foreach (var pos in new[] { "TP9", "AF7", "AF8", "TP10" })
            {
                await UpsertTwinAsync($"electrode-{pos}", "dtmi:cme:Electrode;1", new Dictionary<string, object>
                {
                    ["position"] = pos,
                    ["quality"] = 1.0,
                    ["contactQuality"] = "good",
                    ["lastUpdatedAt"] = DateTime.UtcNow
                });
                await EnsureRelationshipAsync(_opts.HeadbandTwinId, "hasElectrode", $"electrode-{pos}", $"rel-hb-{pos}");
            }

            await EnsureRelationshipAsync(_opts.UserTwinId, "wears", _opts.HeadbandTwinId, "rel-user-wears-headband");

            _logger.LogInformation("Base twins ensured (User + Headband + 4 Electrodes + relationships).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureBaseTwinsAsync failed; ADT mirror will be partial.");
        }
    }

    /// <summary>
    /// Idempotent upsert for one shared Activity-catalogue twin. Bootstrapper calls this
    /// once per ActionDefinition row on startup.
    /// </summary>
    public async Task UpsertActivityTwinAsync(string slug, string displayName, double defaultDifficulty,
        string? icon, bool isSystem)
    {
        try
        {
            await UpsertTwinAsync($"activity-{slug}", "dtmi:cme:Activity;1", new Dictionary<string, object>
            {
                ["slug"] = slug,
                ["displayName"] = displayName,
                ["defaultDifficulty"] = defaultDifficulty,
                ["icon"] = icon ?? "",
                ["isSystem"] = isSystem
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DT activity upsert failed for {Slug}", slug);
        }
    }

    private async Task PushUserAndSessionAsync(CmeResultDto cme)
    {
        var userProps = new Dictionary<string, object>
        {
            ["lastSeenAt"] = DateTime.UtcNow,
            ["cmeSpentTodayVn"] = cme.CmeSessionVn
        };
        if (cme.EngagementIndex.HasValue)     userProps["engagementIndex"]     = cme.EngagementIndex.Value;
        if (cme.CognitiveLoadIndex.HasValue)  userProps["cognitiveLoadIndex"]  = cme.CognitiveLoadIndex.Value;
        if (cme.RelaxationIndex.HasValue)     userProps["relaxationIndex"]     = cme.RelaxationIndex.Value;
        if (cme.AlphaAsymmetryIndex.HasValue) userProps["alphaAsymmetryIndex"] = cme.AlphaAsymmetryIndex.Value;
        if (cme.FlowMinutesToday.HasValue)    userProps["flowMinutesToday"]    = cme.FlowMinutesToday.Value;
        if (cme.BudgetUtilization.HasValue)   userProps["budgetUtilization"]   = cme.BudgetUtilization.Value;
        if (cme.FatigueLevel.HasValue)        userProps["fatigueLevel"]        = cme.FatigueLevel.Value;
        if (!string.IsNullOrEmpty(_activeActivity))     userProps["currentActivitySlug"] = _activeActivity;
        if (!string.IsNullOrEmpty(cme.CurrentSessionId)) userProps["currentSessionId"]    = cme.CurrentSessionId;

        await UpdateThrottledAsync(_opts.UserTwinId, "dtmi:cme:User;1", userProps,
            telemetry: new Dictionary<string, object>
            {
                ["currentPFlow"] = cme.PFlow,
                ["currentCmeRateVnPerSec"] = cme.CmeVn / Math.Max(1.0, cme.TotalWindows == 0 ? 1.0 : 5.0)
            });

        if (_activeSessionTwinId != null)
        {
            var sessProps = new Dictionary<string, object>
            {
                ["cumulativeCmeVn"] = cme.CmeSessionVn,
                ["totalWindows"] = cme.TotalWindows,
                ["meanPFlow"] = cme.PFlow,
                ["complexity"] = _activeComplexity,
                ["activity"] = _activeActivity ?? string.Empty,
                ["inferenceMode"] = _activeInferenceMode
            };
            await UpdateThrottledAsync(_activeSessionTwinId, "dtmi:cme:Session;1", sessProps,
                telemetry: new Dictionary<string, object> { ["cmeRateVnPerSec"] = cme.CmeVn / 5.0 });
        }
    }

    private async Task PushHeadbandAsync(EegWindowDto eeg)
    {
        double minQuality = 1.0;
        bool anyPoor = false;
        if (eeg.ChannelQuality != null && eeg.ChannelQuality.Count > 0)
        {
            minQuality = eeg.ChannelQuality.Values.Min();
            anyPoor = minQuality < 0.5;
        }

        string connectionState;
        if (string.Equals(eeg.SourceMode, "simulator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(eeg.SourceMode, "replay", StringComparison.OrdinalIgnoreCase))
        {
            connectionState = "simulated";
        }
        else if (!eeg.Touching)
        {
            connectionState = "disconnected";
        }
        else if (anyPoor)
        {
            connectionState = "poorContact";
        }
        else
        {
            connectionState = "connected";
        }

        int dropouts = _derived.IncrementDropoutIfTransitioned(_opts.HeadbandTwinId, eeg.Touching);
        double rollingQuality = _derived.RollingSignalQuality(_opts.HeadbandTwinId, minQuality);

        var props = new Dictionary<string, object>
        {
            ["connected"] = eeg.Touching,
            ["sourceMode"] = eeg.SourceMode ?? "live",
            ["connectionState"] = connectionState,
            ["dropoutCountLastHour"] = dropouts,
            ["lastSignalQualityMean"] = rollingQuality
        };
        await UpdateThrottledAsync(_opts.HeadbandTwinId, "dtmi:cme:Headband;1", props);
    }

    private async Task PushElectrodesAsync(EegWindowDto eeg)
    {
        if (eeg.Channels == null) return;
        foreach (var pos in new[] { "TP9", "AF7", "AF8", "TP10" })
        {
            if (!eeg.Channels.TryGetValue(pos, out var bp)) continue;
            var twinId = $"electrode-{pos}";
            var quality = eeg.ChannelQuality?.GetValueOrDefault(pos, eeg.Quality) ?? eeg.Quality;
            string contactQuality = quality >= 0.8 ? "good" : quality >= 0.5 ? "weak" : "none";
            var props = new Dictionary<string, object>
            {
                ["quality"] = quality,
                ["contactQuality"] = contactQuality,
                ["lastUpdatedAt"] = eeg.Timestamp
            };
            var telem = new Dictionary<string, object>
            {
                ["delta"] = bp.Delta, ["theta"] = bp.Theta, ["alpha"] = bp.Alpha,
                ["beta"]  = bp.Beta,  ["gamma"] = bp.Gamma
            };
            await UpdateThrottledAsync(twinId, "dtmi:cme:Electrode;1", props, telemetry: telem);
        }
    }

    /// <summary>
    /// Increments User--practiced-->Activity counters by this session's contribution.
    /// Reads the current relationship (if any), merges deltas, then re-writes via
    /// CreateOrReplaceRelationshipAsync. One ADT write per touched activity per session-end.
    /// </summary>
    private async Task BumpUserPracticedAsync(ActivityUsageDto usage)
    {
        if (string.IsNullOrEmpty(usage.Slug)) return;

        var activityTwinId = $"activity-{usage.Slug}";
        try
        {
            await UpsertTwinAsync(activityTwinId, "dtmi:cme:Activity;1", new Dictionary<string, object>
            {
                ["slug"] = usage.Slug,
                ["displayName"] = usage.DisplayName ?? usage.Slug
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Activity twin upsert (lazy) failed for {Slug}", usage.Slug);
        }

        double existingCmeVn = 0, existingMinutes = 0, existingAvgPFlow = 0;
        int existingSessions = 0;
        var relId = $"rel-user-practiced-{usage.Slug}";

        try
        {
            var resp = await _client.GetRelationshipAsync<BasicRelationship>(_opts.UserTwinId, relId);
            var rel = resp.Value;
            if (rel.Properties.TryGetValue("totalCmeVn", out var v1)) existingCmeVn = ToDouble(v1);
            if (rel.Properties.TryGetValue("totalMinutes", out var v2)) existingMinutes = ToDouble(v2);
            if (rel.Properties.TryGetValue("personalAvgPFlow", out var v3)) existingAvgPFlow = ToDouble(v3);
            if (rel.Properties.TryGetValue("sessionCount", out var v4)) existingSessions = (int)ToDouble(v4);
        }
        catch (RequestFailedException rfe) when (rfe.Status == 404)
        {
            // relationship doesn't exist yet
        }

        double sessionAvgPFlow = usage.WindowCount > 0 ? usage.SumPFlow / usage.WindowCount : 0;
        int newSessions = existingSessions + 1;
        double newAvg = newSessions > 0
            ? ((existingAvgPFlow * existingSessions) + sessionAvgPFlow) / newSessions
            : sessionAvgPFlow;

        var merged = new BasicRelationship
        {
            Id = relId,
            SourceId = _opts.UserTwinId,
            TargetId = activityTwinId,
            Name = "practiced",
            Properties =
            {
                ["totalCmeVn"]      = Math.Round(existingCmeVn + usage.TotalCmeVn, 4),
                ["totalMinutes"]    = Math.Round(existingMinutes + usage.TotalMinutes, 4),
                ["sessionCount"]    = newSessions,
                ["personalAvgPFlow"]= Math.Round(newAvg, 4),
                ["lastUsedAt"]      = DateTime.UtcNow
            }
        };
        await _client.CreateOrReplaceRelationshipAsync(_opts.UserTwinId, relId, merged);
    }

    private static double ToDouble(object? v) => v switch
    {
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.Number
            => je.TryGetDouble(out var x) ? x : 0,
        _ => 0
    };

    private async Task UpdateThrottledAsync(string twinId, string modelId,
        IDictionary<string, object> props, IDictionary<string, object>? telemetry = null)
    {
        var now = DateTime.UtcNow;
        if (_lastSyncByTwin.TryGetValue(twinId, out var last)
            && (now - last).TotalSeconds < _opts.SyncIntervalSeconds)
        {
            return;
        }

        IDictionary<string, object> toSend = props;
        if (_opts.DiffOnly && _lastValuesByTwin.TryGetValue(twinId, out var prev))
        {
            toSend = new Dictionary<string, object>();
            foreach (var (k, v) in props)
            {
                if (!prev.TryGetValue(k, out var pv) || !Equals(pv, v))
                {
                    toSend[k] = v;
                }
            }
            if (toSend.Count == 0 && telemetry == null) return;
        }

        try
        {
            if (toSend.Count > 0)
            {
                var patch = new Azure.JsonPatchDocument();
                foreach (var (k, v) in toSend)
                {
                    patch.AppendAdd($"/{k}", v);
                }
                try
                {
                    await _client.UpdateDigitalTwinAsync(twinId, patch);
                }
                catch (RequestFailedException rfe) when (rfe.Status == 404)
                {
                    await UpsertTwinAsync(twinId, modelId, props);
                }
            }

            if (telemetry != null && telemetry.Count > 0)
            {
                await _client.PublishTelemetryAsync(twinId, Guid.NewGuid().ToString(),
                    System.Text.Json.JsonSerializer.Serialize(telemetry));
            }

            _lastSyncByTwin[twinId] = now;
            _lastValuesByTwin[twinId] = new Dictionary<string, object>(props);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ADT update failed for twin {TwinId}", twinId);
        }
    }

    private async Task UpsertTwinAsync(string twinId, string modelId, IDictionary<string, object> contents)
    {
        var twin = new BasicDigitalTwin
        {
            Id = twinId,
            Metadata = { ModelId = modelId }
        };
        foreach (var (k, v) in contents)
        {
            twin.Contents[k] = v;
        }
        await _client.CreateOrReplaceDigitalTwinAsync(twinId, twin);
        _lastValuesByTwin[twinId] = new Dictionary<string, object>(contents);
        _lastSyncByTwin[twinId] = DateTime.UtcNow;
    }

    private async Task EnsureRelationshipAsync(string source, string name, string target, string relId)
    {
        try
        {
            var rel = new BasicRelationship
            {
                Id = relId,
                SourceId = source,
                TargetId = target,
                Name = name
            };
            await _client.CreateOrReplaceRelationshipAsync(source, relId, rel);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Relationship {Name} {Source}->{Target} create skipped", name, source, target);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
