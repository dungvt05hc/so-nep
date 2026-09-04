namespace NeNep.Domain.Entities;

/// <summary>
/// A conduct classification level. Both the labels and the thresholds are DATA,
/// never hardcoded.
/// Lower secondary school: Chưa đạt(0) &lt; Đạt(1) &lt; Khá(2) &lt; Tốt(3).
/// </summary>
public class ClassificationLevel
{
    public int Id { get; set; }

    /// <summary>CHUA_DAT | DAT | KHA | TOT</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>"Chưa đạt", "Đạt", ... — shown to users, so the value stays in Vietnamese.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rank used when adding or subtracting levels. Higher is better.</summary>
    public int RankOrder { get; set; }

    /// <summary>Lower bound (inclusive). null means no lower bound.</summary>
    public decimal? MinScore { get; set; }

    /// <summary>Upper bound (EXCLUSIVE). null means no upper bound.</summary>
    public decimal? MaxScore { get; set; }

    public string Color { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public ICollection<ConductScore> ScoresByRule { get; set; } = new List<ConductScore>();
    public ICollection<ConductScore> ScoresFinal { get; set; } = new List<ConductScore>();
    public ICollection<ConductAdjustment> AdjBefore { get; set; } = new List<ConductAdjustment>();
    public ICollection<ConductAdjustment> AdjAfter { get; set; } = new List<ConductAdjustment>();
}
