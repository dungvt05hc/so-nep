using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// The 14 rules from the school regulations, stored as data so thresholds can change
/// without touching code.
/// </summary>
public class ConductRule
{
    public int Id { get; set; }

    /// <summary>R01..R14</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public RuleType RuleType { get; set; }

    /// <summary>
    /// Codes that trigger this rule. Grouping is allowed, e.g. R02 = ["N02","N03"].
    /// </summary>
    public List<string> ViolationCodes { get; set; } = new();

    public CompareOp? Operator { get; set; }

    /// <summary>"more than 10 times" becomes operator=GT, thresholdValue=10.</summary>
    public int? ThresholdValue { get; set; }

    public PeriodScope PeriodScope { get; set; }

    public RuleEffect Effect { get; set; }

    /// <summary>Number of levels to move up or down.</summary>
    public int EffectLevels { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public string? Note { get; set; }

    public ICollection<ConductAlert> Alerts { get; set; } = new List<ConductAlert>();
}
