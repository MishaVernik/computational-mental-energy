using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models.FlowDataset;

/// <summary>
/// Raw EEG features per window for dataset building.
/// Used for training classical NN and quantum model with ground truth labels.
/// </summary>
[Table("EegWindowFeatures", Schema = "cme")]
public class EegWindowFeatures
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    public Guid? ActionSpikeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string WindowId { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    // TP9
    public double Delta_TP9 { get; set; }
    public double Theta_TP9 { get; set; }
    public double Alpha_TP9 { get; set; }
    public double Beta_TP9 { get; set; }
    public double Gamma_TP9 { get; set; }

    // AF7
    public double Delta_AF7 { get; set; }
    public double Theta_AF7 { get; set; }
    public double Alpha_AF7 { get; set; }
    public double Beta_AF7 { get; set; }
    public double Gamma_AF7 { get; set; }

    // AF8
    public double Delta_AF8 { get; set; }
    public double Theta_AF8 { get; set; }
    public double Alpha_AF8 { get; set; }
    public double Beta_AF8 { get; set; }
    public double Gamma_AF8 { get; set; }

    // TP10
    public double Delta_TP10 { get; set; }
    public double Theta_TP10 { get; set; }
    public double Alpha_TP10 { get; set; }
    public double Beta_TP10 { get; set; }
    public double Gamma_TP10 { get; set; }

    public double TaskDifficulty { get; set; }
    public double Quality { get; set; }

    /// <summary>Flow label from classical NN or manual (nullable until labeled).</summary>
    public bool? FlowLabel { get; set; }

    /// <summary>Flow probability from classical NN (0-1).</summary>
    public double? FlowProbability { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public virtual Session? Session { get; set; }

    [ForeignKey(nameof(ActionSpikeId))]
    public virtual ActionSpike? ActionSpike { get; set; }
}
