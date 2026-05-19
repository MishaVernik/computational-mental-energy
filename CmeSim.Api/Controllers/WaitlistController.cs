using CmeSim.Api.Data;
using CmeSim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WaitlistController : ControllerBase
{
    private readonly CmeSimDbContext _db;

    public WaitlistController(CmeSimDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult> Signup([FromBody] WaitlistRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { message = "Valid email is required" });

        var exists = await _db.WaitlistSignups.AnyAsync(w => w.Email == req.Email, ct);
        if (exists)
            return Conflict(new { message = "Email already registered" });

        var signup = new WaitlistSignup
        {
            Email = req.Email.Trim().ToLowerInvariant(),
            Role = req.Role,
            HasMuse = req.HasMuse,
        };

        _db.WaitlistSignups.Add(signup);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/waitlist/{signup.Id}", new { id = signup.Id, email = signup.Email });
    }

    [HttpGet]
    public async Task<ActionResult<List<WaitlistSignup>>> List(
        [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var signups = await _db.WaitlistSignups
            .OrderByDescending(w => w.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return Ok(signups);
    }

    [HttpGet("count")]
    public async Task<ActionResult> Count(CancellationToken ct)
    {
        var count = await _db.WaitlistSignups.CountAsync(ct);
        return Ok(new { count });
    }
}

public class WaitlistRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool HasMuse { get; set; }
}
