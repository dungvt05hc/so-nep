using NeNep.Domain.Enums;

namespace NeNep.Domain.Seeding;

/// <summary>One conduct classification level of <see cref="CatalogSeedData.ClassificationLevels"/>.</summary>
/// <param name="Code">CHUA_DAT | DAT | KHA | TOT.</param>
/// <param name="Name">Label shown to users, so the value stays in Vietnamese.</param>
/// <param name="RankOrder">Higher is better; used when adding or subtracting levels.</param>
/// <param name="MinScore">Lower bound, INCLUSIVE. null means no lower bound.</param>
/// <param name="MaxScore">Upper bound, EXCLUSIVE. null means no upper bound.</param>
/// <param name="Color">Badge colour in the reports.</param>
public sealed record SeedClassificationLevel(
    string Code,
    string Name,
    int RankOrder,
    decimal? MinScore,
    decimal? MaxScore,
    string Color);

/// <summary>One catalog group of <see cref="CatalogSeedData.Categories"/>.</summary>
/// <param name="Name">Label shown to users, so the value stays in Vietnamese.</param>
public sealed record SeedCategory(CategoryKind Kind, string Name, int Ordinal);

/// <summary>
/// One violation or commendation code of <see cref="CatalogSeedData.ViolationTypes"/>.
/// </summary>
public sealed record SeedViolationType
{
    /// <summary>C01, N01, V01, H01, B01.</summary>
    public required string Code { get; init; }

    public required CategoryKind Kind { get; init; }

    /// <summary>Wording taken from the school regulations, so the value stays in Vietnamese.</summary>
    public required string Name { get; init; }

    /// <summary>SIGNED points: a bonus is positive (+2), a penalty is negative (-5).</summary>
    public required int Points { get; init; }

    /// <summary>false for N04 (excused absence) and B01/B02 — tracked only, never scored.</summary>
    public bool CountsForScore { get; init; } = true;

    /// <summary>Roles allowed to record this code. Empty means the system computes it.</summary>
    public IReadOnlyList<Role> AllowedRoles { get; init; } = [];

    /// <summary>DECISION Q5: every violation is individual, so this stays false everywhere.</summary>
    public bool IsBulkCapable { get; init; }

    /// <summary>C06 and C07 are computed by the system; nobody enters them by hand.</summary>
    public bool IsAutoComputed { get; init; }

    /// <summary>Maximum occurrences for one student in one week. null means no limit.</summary>
    public int? MaxPerWeek { get; init; }

    public bool RequiresRemediation { get; init; }

    /// <summary>"Trực nhật bổ sung 1 tuần" — shown to users, so the value stays in Vietnamese.</summary>
    public string? RemediationNote { get; init; }

    /// <summary>Days allowed for remediation, counted from the date of the violation.</summary>
    public int? RemediationDays { get; init; }

    /// <summary>Implementation note kept with the row, so it stays in Vietnamese.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// One downgrade or upgrade rule of <see cref="CatalogSeedData.ConductRules"/>.
/// <para>
/// DECISION Q3 + Q4: the app never downgrades or upgrades conduct by itself. The rule
/// engine only DETECTS the situation and raises a <c>ConductAlert</c>; the decision is
/// taken at a school board / homeroom teacher / parent meeting and recorded in
/// <c>ConductAdjustment</c> with the minutes number. Two rules firing at once therefore
/// do not stack into two levels — they are two alerts for the same meeting.
/// </para>
/// </summary>
public sealed record SeedConductRule
{
    /// <summary>R01..R14.</summary>
    public required string Code { get; init; }

    /// <summary>Wording taken from the school regulations, so the value stays in Vietnamese.</summary>
    public required string Name { get; init; }

    public required RuleType RuleType { get; init; }

    /// <summary>Codes that trigger the rule. Grouping is allowed, e.g. R02 = N02 + N03.</summary>
    public required IReadOnlyList<string> ViolationCodes { get; init; }

    /// <summary>The regulations say "more than N times", which is <see cref="CompareOp.GT"/>.</summary>
    public CompareOp? Operator { get; init; }

    public int? ThresholdValue { get; init; }

    public required PeriodScope PeriodScope { get; init; }

    /// <summary>Only a PROPOSAL for the meeting; the app never applies it.</summary>
    public required RuleEffect Effect { get; init; }

    public int EffectLevels { get; init; } = 1;

    /// <summary>Implementation note kept with the row, so it stays in Vietnamese.</summary>
    public string? Note { get; init; }
}
