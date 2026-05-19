using CmeSim.Api.Data;
using CmeSim.Api.Models.FlowDataset;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

/// <summary>
/// API for flow dataset: action spikes and EEG window features.
/// </summary>
[ApiController]
[Route("api/dataset")]
public class DatasetController : ControllerBase
{
    private readonly CmeSimDbContext _db;
    private readonly IFlowClassifierClient _flowClassifier;

    public DatasetController(CmeSimDbContext db, IFlowClassifierClient flowClassifier)
    {
        _db = db;
        _flowClassifier = flowClassifier;
    }

    /// <summary>
    /// Create an action spike (time interval with action type).
    /// </summary>
    [HttpPost("actions")]
    public async Task<ActionResult<ActionSpikeDto>> CreateAction([FromBody] CreateActionSpikeRequest request, CancellationToken ct)
    {
        var spike = new ActionSpike
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ActionType = request.ActionType,
            Description = request.Description
        };
        _db.ActionSpikes.Add(spike);
        await _db.SaveChangesAsync(ct);
        return Ok(MapToDto(spike));
    }

    /// <summary>
    /// List action spikes for a session.
    /// </summary>
    [HttpGet("actions")]
    public async Task<ActionResult<List<ActionSpikeDto>>> ListActions([FromQuery] Guid? sessionId, CancellationToken ct)
    {
        var query = _db.ActionSpikes.AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(a => a.SessionId == sessionId.Value);
        var list = await query.OrderBy(a => a.StartTime).ToListAsync(ct);
        return Ok(list.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Export EEG window features for training.
    /// </summary>
    [HttpGet("windows")]
    public async Task<ActionResult<List<EegWindowFeaturesDto>>> GetWindows(
        [FromQuery] Guid? sessionId,
        [FromQuery] bool? labeled,
        [FromQuery] int limit = 1000,
        CancellationToken ct = default)
    {
        var query = _db.EegWindowFeatures.AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(w => w.SessionId == sessionId.Value);
        if (labeled == true)
            query = query.Where(w => w.FlowLabel != null);
        var list = await query.OrderBy(w => w.Timestamp).Take(limit).ToListAsync(ct);
        return Ok(list.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get label distribution for visualization (total, flowCount, notFlowCount, unlabeledCount).
    /// </summary>
    [HttpGet("label-stats")]
    public async Task<ActionResult<LabelStatsDto>> GetLabelStats(
        [FromQuery] Guid? sessionId,
        CancellationToken ct = default)
    {
        var query = _db.EegWindowFeatures.AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(w => w.SessionId == sessionId.Value);

        var total = await query.CountAsync(ct);
        var flowCount = await query.CountAsync(w => w.FlowLabel == true, ct);
        var notFlowCount = await query.CountAsync(w => w.FlowLabel == false, ct);
        var unlabeledCount = await query.CountAsync(w => w.FlowLabel == null, ct);

        return Ok(new LabelStatsDto(total, flowCount, notFlowCount, unlabeledCount));
    }

    /// <summary>
    /// Run classical NN analysis on unlabeled (or all) windows, write labels to DB.
    /// </summary>
    [HttpPost("analyze-classical")]
    public async Task<ActionResult<AnalyzeClassicalResult>> AnalyzeClassical(
        [FromQuery] Guid? sessionId,
        [FromQuery] bool includeLabeled = false,
        [FromQuery] int limit = 2000,
        CancellationToken ct = default)
    {
        var query = _db.EegWindowFeatures.AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(w => w.SessionId == sessionId.Value);
        if (!includeLabeled)
            query = query.Where(w => w.FlowLabel == null);

        var windows = await query.OrderBy(w => w.Timestamp).Take(limit).ToListAsync(ct);
        var updates = new List<LabelUpdateRequest>();

        foreach (var w in windows)
        {
            var features = new[]
            {
                w.Delta_TP9, w.Theta_TP9, w.Alpha_TP9, w.Beta_TP9, w.Gamma_TP9,
                w.Delta_AF7, w.Theta_AF7, w.Alpha_AF7, w.Beta_AF7, w.Gamma_AF7,
                w.Delta_AF8, w.Theta_AF8, w.Alpha_AF8, w.Beta_AF8, w.Gamma_AF8,
                w.Delta_TP10, w.Theta_TP10, w.Alpha_TP10, w.Beta_TP10, w.Gamma_TP10,
                w.TaskDifficulty, w.Quality
            };
            try
            {
                var (flowProb, flowLabel) = await _flowClassifier.ClassifyAsync(features, ct);
                updates.Add(new LabelUpdateRequest(w.Id, flowLabel, flowProb));
            }
            catch
            {
                // Skip on classifier failure
            }
        }

        foreach (var u in updates)
        {
            var w = await _db.EegWindowFeatures.FindAsync(new object[] { u.Id }, ct);
            if (w != null)
            {
                w.FlowLabel = u.FlowLabel;
                w.FlowProbability = u.FlowProbability;
            }
        }
        await _db.SaveChangesAsync(ct);

        return Ok(new AnalyzeClassicalResult(windows.Count, updates.Count));
    }

    /// <summary>
    /// Bulk update flow labels (e.g. from classical NN or manual).
    /// </summary>
    [HttpPost("label-batch")]
    public async Task<ActionResult> LabelBatch([FromBody] List<LabelUpdateRequest> updates, CancellationToken ct)
    {
        foreach (var u in updates)
        {
            var w = await _db.EegWindowFeatures.FindAsync(new object[] { u.Id }, ct);
            if (w == null) continue;
            w.FlowLabel = u.FlowLabel;
            w.FlowProbability = u.FlowProbability;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { updated = updates.Count });
    }

    /// <summary>
    /// Session energy forecast: current session's CME, rate, and 16h projection.
    /// "If I keep doing exactly this for a full 16h active day, how much energy?"
    /// Pass ?sessionId= for a specific session, otherwise uses the latest.
    /// </summary>
    [HttpGet("energy-forecast")]
    public async Task<ActionResult<EnergyForecastDto>> GetEnergyForecast(
        [FromQuery] Guid? sessionId = null, CancellationToken ct = default)
    {
        // Resolve session
        Guid resolvedSessionId;
        if (sessionId.HasValue)
        {
            resolvedSessionId = sessionId.Value;
        }
        else
        {
            var latest = await _db.Sessions.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(ct);
            if (latest == null)
                return Ok(new EnergyForecastDto(0, 0, 0, 16, 0, 0, new List<ActionEnergyDto>()));
            resolvedSessionId = latest.Id;
        }

        var session = await _db.Sessions.FindAsync(new object[] { resolvedSessionId }, ct);
        if (session == null)
            return Ok(new EnergyForecastDto(0, 0, 0, 16, 0, 0, new List<ActionEnergyDto>()));

        var sessionWindows = await _db.CmeWindowResults
            .Where(c => c.SessionId == resolvedSessionId)
            .OrderBy(c => c.ComputedAt)
            .Select(c => new { c.CmeValue, c.ComputedAt, c.ActionSpikeId })
            .ToListAsync(ct);

        double sessionSpent = sessionWindows.Sum(w => w.CmeValue);
        int totalWindows = sessionWindows.Count;

        // Session elapsed time
        var sessionEnd = session.EndedAt ?? DateTime.UtcNow;
        double sessionMinutes = (sessionEnd - session.StartedAt).TotalMinutes;

        // Current rate: from last 30 windows
        var recentWindows = sessionWindows.TakeLast(30).ToList();
        double currentRate = 0;
        if (recentWindows.Count >= 2)
        {
            var span = (recentWindows.Last().ComputedAt - recentWindows.First().ComputedAt).TotalMinutes;
            currentRate = span > 0 ? recentWindows.Sum(w => w.CmeValue) / span : 0;
        }
        else if (totalWindows > 0 && sessionMinutes > 0)
        {
            currentRate = sessionSpent / sessionMinutes;
        }

        // 16h projection: "doing this for a full active day"
        double projected16h = currentRate * 60 * 16;

        // Per-action breakdown (within this session)
        var spikeIds = sessionWindows
            .Where(w => w.ActionSpikeId.HasValue)
            .Select(w => w.ActionSpikeId!.Value)
            .Distinct()
            .ToList();

        var spikes = spikeIds.Count > 0
            ? await _db.ActionSpikes
                .Where(s => spikeIds.Contains(s.Id))
                .Include(s => s.ActionDefinition)
                .ToListAsync(ct)
            : new List<Models.FlowDataset.ActionSpike>();

        var spikeLookup = spikes.ToDictionary(s => s.Id);

        var perAction = sessionWindows
            .Where(w => w.ActionSpikeId.HasValue && spikeLookup.ContainsKey(w.ActionSpikeId.Value))
            .GroupBy(w => w.ActionSpikeId!.Value)
            .Select(g =>
            {
                var spike = spikeLookup[g.Key];
                var windows = g.OrderBy(w => w.ComputedAt).ToList();
                double spent = windows.Sum(w => w.CmeValue);
                double minutes = windows.Count > 1
                    ? (windows.Last().ComputedAt - windows.First().ComputedAt).TotalMinutes
                    : windows.Count * 5.0 / 60.0;
                double avgRate = minutes > 0 ? spent / minutes : 0;
                return new ActionEnergyDto(
                    spike.ActionDefinition?.Name ?? spike.ActionType,
                    spike.ActionDefinition?.Slug ?? spike.ActionType,
                    Math.Round(spent, 2),
                    Math.Round(avgRate, 2),
                    Math.Round(minutes, 1),
                    windows.Count);
            })
            .OrderByDescending(a => a.Spent)
            .ToList();

        return Ok(new EnergyForecastDto(
            Math.Round(sessionSpent, 2),
            Math.Round(currentRate, 2),
            Math.Round(projected16h, 2),
            Math.Round(sessionMinutes, 1),
            totalWindows,
            Math.Round(sessionMinutes, 1),
            perAction));
    }

    /// <summary>
    /// Day journal: per-session breakdown with activity-weighted energy budgets.
    /// Budget = baseBudgetPerHour × sessionHours × weightedDifficulty
    /// A session of only breathing has low difficulty → low budget.
    /// A session of coding + thinking has high difficulty → high budget.
    /// </summary>
    [HttpGet("day-journal")]
    public async Task<ActionResult<DayJournalDto>> GetDayJournal(CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var todaySessions = await _db.Sessions
            .Where(s => s.StartedAt >= todayUtc || (s.EndedAt == null && s.StartedAt >= todayUtc.AddDays(-1)))
            .OrderBy(s => s.StartedAt)
            .ToListAsync(ct);

        if (todaySessions.Count == 0)
            todaySessions = await _db.Sessions.OrderByDescending(s => s.StartedAt).Take(1).ToListAsync(ct);

        const double baseBudgetPerHourLow = 3000;   // Vn/hr for difficulty=0 (resting)
        const double baseBudgetPerHourHigh = 30000;  // Vn/hr for difficulty=1 (deep work)

        var sessionJournals = new List<SessionJournalDto>();
        double dayTotalCme = 0, dayTotalMin = 0, dayAvgDiffSum = 0;
        int dayTotalSegments = 0;
        double dayFlowShareSum = 0;
        var dayActionAgg = new Dictionary<string, (string Name, string Slug, double Spent, double Minutes, int Windows)>();

        foreach (var session in todaySessions)
        {
            var endedAt = session.EndedAt ?? DateTime.UtcNow;
            var durationMin = (endedAt - session.StartedAt).TotalMinutes;

            var windows = await _db.CmeWindowResults
                .Where(c => c.SessionId == session.Id)
                .OrderBy(c => c.ComputedAt)
                .Select(c => new { c.CmeValue, c.PFlow, c.ComputedAt, c.ActionSpikeId })
                .ToListAsync(ct);

            double cmeTotal = windows.Sum(w => w.CmeValue);
            double avgPFlow = windows.Count > 0 ? windows.Average(w => w.PFlow) : 0;
            int flowCount = windows.Count(w => w.PFlow >= 0.85);
            double flowShare = windows.Count > 0 ? (double)flowCount / windows.Count : 0;
            double avgRate = durationMin > 0 ? cmeTotal / durationMin : 0;

            var spikeIds = windows
                .Where(w => w.ActionSpikeId.HasValue)
                .Select(w => w.ActionSpikeId!.Value)
                .Distinct().ToList();

            var spikes = spikeIds.Count > 0
                ? await _db.ActionSpikes
                    .Where(s => spikeIds.Contains(s.Id))
                    .Include(s => s.ActionDefinition)
                    .ToListAsync(ct)
                : new List<ActionSpike>();

            var spikeLookup = spikes.ToDictionary(s => s.Id);

            var segments = new List<SegmentJournalDto>();
            double weightedDiffSum = 0, weightedDiffTime = 0;

            foreach (var spike in spikes.OrderBy(s => s.StartTime))
            {
                var segWindows = windows.Where(w => w.ActionSpikeId == spike.Id).OrderBy(w => w.ComputedAt).ToList();
                double segCme = segWindows.Sum(w => w.CmeValue);
                double segAvgPFlow = segWindows.Count > 0 ? segWindows.Average(w => w.PFlow) : 0;
                double segDurMin = (spike.EndTime - spike.StartTime).TotalMinutes;
                double difficulty = spike.ActionDefinition?.DefaultDifficulty ?? 0.5;
                double cmePerMin = segDurMin > 0 ? segCme / segDurMin : 0;
                double pctOfSession = cmeTotal > 0 ? segCme / cmeTotal * 100 : 0;

                segments.Add(new SegmentJournalDto(
                    spike.Id,
                    spike.ActionDefinition?.Name ?? spike.ActionType,
                    spike.ActionDefinition?.Slug ?? spike.ActionType,
                    spike.Description, difficulty,
                    spike.StartTime, spike.EndTime,
                    Math.Round(segDurMin, 1), segWindows.Count,
                    Math.Round(segCme, 1), Math.Round(segAvgPFlow, 3),
                    Math.Round(cmePerMin, 1), Math.Round(pctOfSession, 1)));

                weightedDiffSum += difficulty * segDurMin;
                weightedDiffTime += segDurMin;

                var actionKey = spike.ActionDefinition?.Slug ?? spike.ActionType;
                if (!dayActionAgg.ContainsKey(actionKey))
                    dayActionAgg[actionKey] = (spike.ActionDefinition?.Name ?? spike.ActionType, actionKey, 0, 0, 0);
                var (n, sl, sp, mi, wi) = dayActionAgg[actionKey];
                dayActionAgg[actionKey] = (n, sl, sp + segCme, mi + segDurMin, wi + segWindows.Count);
            }

            double weightedDifficulty = weightedDiffTime > 0 ? weightedDiffSum / weightedDiffTime : 0.5;
            double sessionHours = Math.Max(durationMin / 60.0, 0.1);
            double budgetPerHour = baseBudgetPerHourLow + (baseBudgetPerHourHigh - baseBudgetPerHourLow) * weightedDifficulty;
            double sessionBudget = budgetPerHour * sessionHours;
            double budgetUsedPct = sessionBudget > 0 ? cmeTotal / sessionBudget * 100 : 0;

            sessionJournals.Add(new SessionJournalDto(
                session.Id, session.StartedAt, session.EndedAt,
                Math.Round(durationMin, 1), windows.Count,
                Math.Round(cmeTotal, 1), Math.Round(avgRate, 1),
                Math.Round(avgPFlow, 3), Math.Round(flowShare, 3),
                Math.Round(weightedDifficulty, 2), Math.Round(sessionBudget, 0),
                Math.Round(Math.Min(budgetUsedPct, 999), 1),
                segments));

            dayTotalCme += cmeTotal;
            dayTotalMin += durationMin;
            dayTotalSegments += segments.Count;
            dayAvgDiffSum += weightedDifficulty * durationMin;
            dayFlowShareSum += flowShare * durationMin;
        }

        double dayAvgDiff = dayTotalMin > 0 ? dayAvgDiffSum / dayTotalMin : 0.5;
        double dayAvgFlow = dayTotalMin > 0 ? dayFlowShareSum / dayTotalMin : 0;
        double dayHours = Math.Max(dayTotalMin / 60.0, 0.1);
        double dayBudgetPerHour = baseBudgetPerHourLow + (baseBudgetPerHourHigh - baseBudgetPerHourLow) * dayAvgDiff;
        double dayBudget = dayBudgetPerHour * dayHours;
        double dayBudgetUsedPct = dayBudget > 0 ? dayTotalCme / dayBudget * 100 : 0;

        var topActivities = dayActionAgg.Values
            .Select(a => new ActionEnergyDto(a.Name, a.Slug, Math.Round(a.Spent, 1),
                a.Minutes > 0 ? Math.Round(a.Spent / a.Minutes, 1) : 0,
                Math.Round(a.Minutes, 1), a.Windows))
            .OrderByDescending(a => a.Spent)
            .Take(10).ToList();

        var daySummary = new DaySummaryDto(
            Math.Round(dayTotalCme, 1), Math.Round(dayTotalMin, 1),
            todaySessions.Count, dayTotalSegments,
            Math.Round(dayAvgDiff, 2), Math.Round(dayAvgFlow, 3),
            Math.Round(dayBudget, 0), Math.Round(Math.Min(dayBudgetUsedPct, 999), 1),
            topActivities);

        return Ok(new DayJournalDto(sessionJournals, daySummary));
    }

    /// <summary>
    /// Annotate a segment: pick an activity, time range, and describe what happened.
    /// Links to ActionDefinition for rich categorization. Computes CME summary for the segment.
    /// </summary>
    [HttpPost("segments")]
    public async Task<ActionResult<SegmentDto>> AnnotateSegment([FromBody] AnnotateSegmentRequest req, CancellationToken ct)
    {
        if (req.ActionDefinitionId == null && string.IsNullOrWhiteSpace(req.ActionType))
            return BadRequest("Either actionDefinitionId or actionType is required");

        ActionDefinition? actionDef = null;
        string actionType = req.ActionType ?? "";
        if (req.ActionDefinitionId.HasValue)
        {
            actionDef = await _db.ActionDefinitions.FindAsync(new object[] { req.ActionDefinitionId.Value }, ct);
            if (actionDef != null) actionType = actionDef.Slug;
        }

        // Resolve session: use provided or find the latest
        Guid sessionId;
        if (req.SessionId.HasValue)
        {
            sessionId = req.SessionId.Value;
        }
        else
        {
            var latest = await _db.Sessions.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(ct);
            if (latest == null) return BadRequest("No sessions found");
            sessionId = latest.Id;
        }

        // Default times: if not provided, use "last N minutes"
        var endTime = req.EndTime ?? DateTime.UtcNow;
        var startTime = req.StartTime ?? endTime.AddMinutes(-(req.DurationMinutes ?? 5));

        var spike = new ActionSpike
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            ActionType = actionType,
            ActionDefinitionId = req.ActionDefinitionId,
            Description = req.Description
        };
        _db.ActionSpikes.Add(spike);
        await _db.SaveChangesAsync(ct);

        // Link matching CmeWindowResults to this spike so per-action energy breakdown works
        var segmentWindows = await _db.CmeWindowResults
            .Where(c => c.SessionId == sessionId && c.ComputedAt >= startTime && c.ComputedAt <= endTime)
            .OrderBy(c => c.ComputedAt)
            .ToListAsync(ct);

        foreach (var w in segmentWindows)
            w.ActionSpikeId = spike.Id;
        await _db.SaveChangesAsync(ct);

        double cmeTotalVn = segmentWindows.Sum(w => w.CmeValue);
        double avgPFlow = segmentWindows.Count > 0 ? segmentWindows.Average(w => w.PFlow) : 0;

        return Ok(new SegmentDto(
            spike.Id, sessionId, startTime, endTime,
            actionDef?.Name ?? actionType, actionType, req.Description,
            actionDef?.DefaultDifficulty ?? 0.5,
            segmentWindows.Count, Math.Round(cmeTotalVn, 2), Math.Round(avgPFlow, 3),
            spike.CreatedAt));
    }

    /// <summary>
    /// List annotated segments for a session, with CME summary for each.
    /// </summary>
    [HttpGet("segments")]
    public async Task<ActionResult<List<SegmentDto>>> ListSegments(
        [FromQuery] Guid? sessionId,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var query = _db.ActionSpikes
            .Include(a => a.ActionDefinition)
            .AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(a => a.SessionId == sessionId.Value);

        var spikes = await query.OrderByDescending(a => a.StartTime).Take(limit).ToListAsync(ct);

        var result = new List<SegmentDto>();
        foreach (var s in spikes)
        {
            var windowCount = await _db.CmeWindowResults
                .CountAsync(c => c.SessionId == s.SessionId && c.ComputedAt >= s.StartTime && c.ComputedAt <= s.EndTime, ct);
            var cmeSummary = windowCount > 0
                ? await _db.CmeWindowResults
                    .Where(c => c.SessionId == s.SessionId && c.ComputedAt >= s.StartTime && c.ComputedAt <= s.EndTime)
                    .SumAsync(c => c.CmeValue, ct)
                : 0;
            var avgPFlow = windowCount > 0
                ? await _db.CmeWindowResults
                    .Where(c => c.SessionId == s.SessionId && c.ComputedAt >= s.StartTime && c.ComputedAt <= s.EndTime)
                    .AverageAsync(c => c.PFlow, ct)
                : 0;

            result.Add(new SegmentDto(
                s.Id, s.SessionId, s.StartTime, s.EndTime,
                s.ActionDefinition?.Name ?? s.ActionType, s.ActionType, s.Description,
                s.ActionDefinition?.DefaultDifficulty ?? 0.5,
                windowCount, Math.Round(cmeSummary, 2), Math.Round(avgPFlow, 3),
                s.CreatedAt));
        }

        return Ok(result);
    }

    /// <summary>
    /// Cross-session per-activity aggregates over the last N days. Used by the
    /// dashboard's "compare activities" view to rank activities by CME rate, pFlow,
    /// and minutes spent. Window-level join (not segment metadata) so peakPFlow is exact.
    /// </summary>
    [HttpGet("/api/activities/compare")]
    public async Task<ActionResult<List<ActivityCompareDto>>> CompareActivities(
        [FromQuery] string? userId = null,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Max(1, days));

        var sessionQuery = _db.Sessions.Where(s => s.StartedAt >= since);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            sessionQuery = sessionQuery.Where(s => s.UserId == userId);
        }
        var sessionIds = await sessionQuery.Select(s => s.Id).ToListAsync(ct);
        if (sessionIds.Count == 0)
        {
            return Ok(new List<ActivityCompareDto>());
        }

        var spikes = await _db.ActionSpikes
            .Where(a => sessionIds.Contains(a.SessionId))
            .Include(a => a.ActionDefinition)
            .ToListAsync(ct);
        if (spikes.Count == 0)
        {
            return Ok(new List<ActivityCompareDto>());
        }

        var spikeIds = spikes.Select(s => s.Id).ToList();
        var windows = await _db.CmeWindowResults
            .Where(c => c.ActionSpikeId != null && spikeIds.Contains(c.ActionSpikeId!.Value))
            .Select(c => new { c.ActionSpikeId, c.CmeValue, c.PFlow, c.ComputedAt })
            .ToListAsync(ct);

        var spikeLookup = spikes.ToDictionary(s => s.Id);
        var windowsBySpike = windows.GroupBy(w => w.ActionSpikeId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var byActivity = new Dictionary<string, ActivityCompareDto>();
        foreach (var spike in spikes)
        {
            var slug = spike.ActionDefinition?.Slug ?? spike.ActionType;
            var name = spike.ActionDefinition?.Name ?? spike.ActionType;
            if (string.IsNullOrEmpty(slug)) continue;

            if (!byActivity.TryGetValue(slug, out var row))
            {
                row = new ActivityCompareDto
                {
                    Slug = slug,
                    DisplayName = name,
                    Icon = spike.ActionDefinition?.Icon
                };
                byActivity[slug] = row;
            }

            var ws = windowsBySpike.GetValueOrDefault(spike.Id) ?? new();
            double cmeSum = ws.Sum(w => w.CmeValue);
            double minutes = (spike.EndTime - spike.StartTime).TotalMinutes;
            if (minutes <= 0) minutes = ws.Count * 5.0 / 60.0;

            row.TotalCmeVn += cmeSum;
            row.TotalMinutes += minutes;
            row.WindowCount += ws.Count;
            row.SumPFlow += ws.Sum(w => w.PFlow);
            row.SessionsSet.Add(spike.SessionId);
            if (ws.Count > 0)
            {
                double localPeak = ws.Max(w => w.PFlow);
                if (localPeak > row.PeakPFlow) row.PeakPFlow = localPeak;
            }
            var endTime = spike.EndTime > row.LastUsedAt ? spike.EndTime : row.LastUsedAt;
            row.LastUsedAt = endTime;
        }

        var result = byActivity.Values
            .Select(r =>
            {
                r.SessionCount = r.SessionsSet.Count;
                r.AvgPFlow = r.WindowCount > 0 ? Math.Round(r.SumPFlow / r.WindowCount, 4) : 0;
                r.TotalCmeVn = Math.Round(r.TotalCmeVn, 2);
                r.TotalMinutes = Math.Round(r.TotalMinutes, 1);
                r.PeakPFlow = Math.Round(r.PeakPFlow, 4);
                return r;
            })
            .OrderByDescending(r => r.TotalCmeVn)
            .ToList();

        return Ok(result);
    }

    private static ActionSpikeDto MapToDto(ActionSpike a) => new(
        a.Id, a.SessionId, a.StartTime, a.EndTime, a.ActionType, a.Description, a.CreatedAt);

    private static EegWindowFeaturesDto MapToDto(EegWindowFeatures w) => new(
        w.Id, w.SessionId, w.ActionSpikeId, w.WindowId, w.Timestamp, w.TaskDifficulty, w.Quality,
        w.FlowLabel, w.FlowProbability,
        new[] { w.Delta_TP9, w.Theta_TP9, w.Alpha_TP9, w.Beta_TP9, w.Gamma_TP9,
            w.Delta_AF7, w.Theta_AF7, w.Alpha_AF7, w.Beta_AF7, w.Gamma_AF7,
            w.Delta_AF8, w.Theta_AF8, w.Alpha_AF8, w.Beta_AF8, w.Gamma_AF8,
            w.Delta_TP10, w.Theta_TP10, w.Alpha_TP10, w.Beta_TP10, w.Gamma_TP10 });
}

public record CreateActionSpikeRequest(Guid SessionId, DateTime StartTime, DateTime EndTime, string ActionType, string? Description = null);
public record ActionSpikeDto(Guid Id, Guid SessionId, DateTime StartTime, DateTime EndTime, string ActionType, string? Description, DateTime CreatedAt);
public record EegWindowFeaturesDto(Guid Id, Guid SessionId, Guid? ActionSpikeId, string WindowId, DateTime Timestamp, double TaskDifficulty, double Quality, bool? FlowLabel, double? FlowProbability, double[] Features);
public record LabelUpdateRequest(Guid Id, bool FlowLabel, double? FlowProbability = null);
public record LabelStatsDto(int Total, int FlowCount, int NotFlowCount, int UnlabeledCount);
public record AnalyzeClassicalResult(int Analyzed, int Labeled);
public record EnergyForecastDto(
    double EnergySpentToday, double CurrentRatePerMin, double ProjectedTotal,
    double RemainingHours, int TotalWindows, double SessionMinutes,
    List<ActionEnergyDto> PerAction);
public record ActionEnergyDto(
    string ActionName, string ActionSlug, double Spent, double AvgRatePerMin, double Minutes, int Windows);

public record DayJournalDto(
    List<SessionJournalDto> Sessions, DaySummaryDto DaySummary);

public record SessionJournalDto(
    Guid SessionId, DateTime StartedAt, DateTime? EndedAt,
    double DurationMin, int TotalWindows,
    double CmeTotal, double AvgCmeRate, double AvgPFlow, double FlowShare,
    double WeightedDifficulty, double SessionBudget, double BudgetUsedPct,
    List<SegmentJournalDto> Segments);

public record SegmentJournalDto(
    Guid Id, string ActionName, string ActionSlug, string? Description,
    double Difficulty, DateTime StartTime, DateTime EndTime,
    double DurationMin, int WindowCount, double CmeTotal,
    double AvgPFlow, double CmePerMin, double PctOfSession);

public record DaySummaryDto(
    double TotalCme, double TotalMinutes, int TotalSessions, int TotalSegments,
    double AvgDifficulty, double AvgFlowShare, double DayBudget, double BudgetUsedPct,
    List<ActionEnergyDto> TopActivities);

public record AnnotateSegmentRequest(
    Guid? ActionDefinitionId = null, string? ActionType = null, string? Description = null,
    Guid? SessionId = null, DateTime? StartTime = null, DateTime? EndTime = null, double? DurationMinutes = null);
public record SegmentDto(
    Guid Id, Guid SessionId, DateTime StartTime, DateTime EndTime,
    string ActionName, string ActionSlug, string? Description, double Difficulty,
    int WindowCount, double CmeTotalVn, double AvgPFlow, DateTime CreatedAt);

/// <summary>
/// Mutable row used to accumulate cross-session per-activity stats. The <see cref="SessionsSet"/>
/// is internal to the aggregator and not serialized.
/// </summary>
public class ActivityCompareDto
{
    public string Slug { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Icon { get; set; }
    public double TotalCmeVn { get; set; }
    public double TotalMinutes { get; set; }
    public int SessionCount { get; set; }
    public double AvgPFlow { get; set; }
    public double PeakPFlow { get; set; }
    public DateTime LastUsedAt { get; set; }
    public int WindowCount { get; set; }

    [System.Text.Json.Serialization.JsonIgnore] public double SumPFlow { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public HashSet<Guid> SessionsSet { get; set; } = new();
}
