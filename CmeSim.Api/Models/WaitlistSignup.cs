using System.ComponentModel.DataAnnotations;

namespace CmeSim.Api.Models;

public class WaitlistSignup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Role { get; set; }

    public bool HasMuse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
