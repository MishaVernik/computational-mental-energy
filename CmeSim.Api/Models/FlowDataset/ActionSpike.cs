using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models.FlowDataset;

/// <summary>
/// Time interval [StartTime, EndTime] with action type – what the user was doing.
/// Enables training: "EEG during action X → flow state".
/// </summary>
[Table("ActionSpikes", Schema = "cme")]
public class ActionSpike
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? ActionDefinitionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public virtual Session? Session { get; set; }

    [ForeignKey(nameof(ActionDefinitionId))]
    public virtual ActionDefinition? ActionDefinition { get; set; }

    public virtual ICollection<EegWindowFeatures> EegWindowFeatures { get; set; } = new List<EegWindowFeatures>();
}
