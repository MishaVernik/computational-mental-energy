using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models.FlowDataset;

[Table("ActionDefinitions", Schema = "cme")]
public class ActionDefinition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ParentId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Slug { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public double DefaultDifficulty { get; set; } = 0.5;

    [MaxLength(50)]
    public string? Icon { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentId))]
    public virtual ActionDefinition? Parent { get; set; }

    public virtual ICollection<ActionDefinition> Children { get; set; } = new List<ActionDefinition>();
}
