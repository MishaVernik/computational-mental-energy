using CmeSim.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

/// <summary>
/// API for listing EEG recording sessions with window counts.
/// </summary>
[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly CmeSimDbContext _db;

    public SessionsController(CmeSimDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List sessions with window counts for session selector.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> ListSessions(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var sessions = await _db.Sessions
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var counts = await _db.EegWindowFeatures
            .Where(w => sessionIds.Contains(w.SessionId))
            .GroupBy(w => w.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countDict = counts.ToDictionary(c => c.SessionId, c => c.Count);
        return Ok(sessions.Select(s => new SessionDto(s.Id, s.UserId, s.StartedAt, countDict.GetValueOrDefault(s.Id, 0))).ToList());
    }
    [HttpGet("spike-stats/{spikeId:guid}")]
    public async Task<ActionResult<SpikeStatsDto>> GetSpikeStats(Guid spikeId, CancellationToken ct = default)
    {
        var windows = await _db.CmeWindowResults
            .Where(w => w.ActionSpikeId == spikeId)
            .ToListAsync(ct);

        if (windows.Count == 0)
            return Ok(new SpikeStatsDto(0, 0, 0, 0));

        double meanPFlow = windows.Average(w => w.PFlow);
        double totalVn = windows.Sum(w => Math.Max(0, w.CmeValue));
        double duration = windows.Count * 5.0;
        double meanRate = duration > 0 ? totalVn / duration : 0;

        return Ok(new SpikeStatsDto(windows.Count, meanPFlow, meanRate, totalVn));
    }
}

public record SessionDto(Guid Id, string UserId, DateTime StartedAt, int WindowCount);
public record SpikeStatsDto(int WindowCount, double MeanPFlow, double MeanCmeRate, double TotalCmeVn);
