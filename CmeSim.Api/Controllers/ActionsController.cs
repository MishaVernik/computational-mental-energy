using CmeSim.Api.Data;
using CmeSim.Api.Models.FlowDataset;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

[ApiController]
[Route("api/actions")]
public class ActionsController : ControllerBase
{
    private readonly CmeSimDbContext _db;

    public ActionsController(CmeSimDbContext db) => _db = db;

    /// <summary>
    /// Returns the full hierarchical tree of active action definitions.
    /// Top-level categories have children nested inside.
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<List<ActionTreeNode>>> GetTree(CancellationToken ct)
    {
        var all = await _db.ActionDefinitions
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        var lookup = all.ToLookup(a => a.ParentId);
        var roots = lookup[null]
            .Select(c => BuildNode(c, lookup))
            .OrderBy(n => n.Name)
            .ToList();

        return Ok(roots);
    }

    /// <summary>
    /// Flat list with optional search and category filter.
    /// </summary>
    [HttpGet("flat")]
    public async Task<ActionResult<List<ActionFlatDto>>> GetFlat(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
    {
        var query = _db.ActionDefinitions.Where(a => a.IsActive);

        if (categoryId.HasValue)
            query = query.Where(a => a.ParentId == categoryId.Value || a.Id == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(s) || a.Slug.ToLower().Contains(s));
        }

        var list = await query.OrderBy(a => a.Name).ToListAsync(ct);
        return Ok(list.Select(a => new ActionFlatDto(
            a.Id, a.ParentId, a.Name, a.Slug, a.Description,
            a.DefaultDifficulty, a.Icon, a.IsSystem)).ToList());
    }

    /// <summary>
    /// Create a custom (user-defined) action.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ActionFlatDto>> Create([FromBody] CreateActionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Name is required");

        var slug = GenerateSlug(req.Name);
        if (await _db.ActionDefinitions.AnyAsync(a => a.Slug == slug, ct))
            return Conflict($"Action with slug '{slug}' already exists");

        var entity = new ActionDefinition
        {
            Id = Guid.NewGuid(),
            ParentId = req.ParentId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description,
            DefaultDifficulty = Math.Clamp(req.DefaultDifficulty ?? 0.5, 0, 1),
            Icon = req.Icon,
            IsSystem = false,
            IsActive = true
        };

        _db.ActionDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetFlat), new ActionFlatDto(
            entity.Id, entity.ParentId, entity.Name, entity.Slug, entity.Description,
            entity.DefaultDifficulty, entity.Icon, entity.IsSystem));
    }

    /// <summary>
    /// Update name, description, difficulty, icon of an action.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ActionFlatDto>> Update(Guid id, [FromBody] UpdateActionRequest req, CancellationToken ct)
    {
        var entity = await _db.ActionDefinitions.FindAsync(new object[] { id }, ct);
        if (entity == null || !entity.IsActive)
            return NotFound();

        if (req.Name != null) entity.Name = req.Name.Trim();
        if (req.Description != null) entity.Description = req.Description;
        if (req.DefaultDifficulty.HasValue) entity.DefaultDifficulty = Math.Clamp(req.DefaultDifficulty.Value, 0, 1);
        if (req.Icon != null) entity.Icon = req.Icon;

        await _db.SaveChangesAsync(ct);

        return Ok(new ActionFlatDto(
            entity.Id, entity.ParentId, entity.Name, entity.Slug, entity.Description,
            entity.DefaultDifficulty, entity.Icon, entity.IsSystem));
    }

    /// <summary>
    /// Soft-delete an action (set IsActive=false). Blocked for system actions.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.ActionDefinitions.FindAsync(new object[] { id }, ct);
        if (entity == null)
            return NotFound();
        if (entity.IsSystem)
            return BadRequest("Cannot delete system-defined actions");

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ActionTreeNode BuildNode(ActionDefinition def, ILookup<Guid?, ActionDefinition> lookup)
    {
        var children = lookup[def.Id]
            .Select(c => BuildNode(c, lookup))
            .OrderBy(n => n.Name)
            .ToList();

        return new ActionTreeNode(
            def.Id, def.Name, def.Slug, def.Description,
            def.DefaultDifficulty, def.Icon, def.IsSystem,
            children.Count > 0 ? children : null);
    }

    private static string GenerateSlug(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('/', '-')
            .Replace("(", "").Replace(")", "")
            .Replace("--", "-")
            .TrimEnd('-');
}

// ─── DTOs ────────────────────────────────────────────────────────

public record ActionTreeNode(
    Guid Id, string Name, string Slug, string? Description,
    double DefaultDifficulty, string? Icon, bool IsSystem,
    List<ActionTreeNode>? Children);

public record ActionFlatDto(
    Guid Id, Guid? ParentId, string Name, string Slug, string? Description,
    double DefaultDifficulty, string? Icon, bool IsSystem);

public record CreateActionRequest(
    string Name, Guid? ParentId = null, string? Description = null,
    double? DefaultDifficulty = null, string? Icon = null);

public record UpdateActionRequest(
    string? Name = null, string? Description = null,
    double? DefaultDifficulty = null, string? Icon = null);
