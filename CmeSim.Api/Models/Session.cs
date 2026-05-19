using System.ComponentModel.DataAnnotations;

namespace CmeSim.Api.Models;

/// <summary>
/// Represents an EEG recording session.
/// In the real system, this would track a continuous brain monitoring session.
/// </summary>
public class Session
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    // Navigation properties
    public virtual ICollection<InferenceRequestLog> InferenceRequests { get; set; } = new List<InferenceRequestLog>();
    public virtual ICollection<CmeWindowResult> CmeResults { get; set; } = new List<CmeWindowResult>();
}


