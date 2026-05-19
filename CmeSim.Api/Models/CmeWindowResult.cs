using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmeSim.Api.Models;

/// <summary>
/// Stores the computed CME (Countable Mental Energy) for each time window.
/// </summary>
public class CmeWindowResult
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string WindowId { get; set; } = string.Empty;

    public DateTime ComputedAt { get; set; }

    /// <summary>
    /// Computed CME value: f(energy, p_flow, task_difficulty).
    /// </summary>
    public double CmeValue { get; set; }

    /// <summary>
    /// Probability of "flow" mental state from quantum classifier (0-1).
    /// </summary>
    public double PFlow { get; set; }

    /// <summary>
    /// Number of quantum circuit shots used.
    /// </summary>
    public int ShotsUsed { get; set; }

    /// <summary>
    /// Quantum circuit depth.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Flow label from classical NN or manual (nullable until labeled).
    /// </summary>
    public bool? FlowLabel { get; set; }

    /// <summary>
    /// Flow probability from classical NN (0-1).
    /// </summary>
    public double? FlowProbability { get; set; }

    public Guid? ActionSpikeId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SessionId))]
    public virtual Session? Session { get; set; }

    [ForeignKey(nameof(ActionSpikeId))]
    public virtual FlowDataset.ActionSpike? ActionSpike { get; set; }
}


