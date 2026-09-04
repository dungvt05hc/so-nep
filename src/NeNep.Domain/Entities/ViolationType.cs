using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>The standard scoring catalog: one violation or commendation code.</summary>
public class ViolationType
{
    public int Id { get; set; }

    /// <summary>C01, N01, V01, H01, B01</summary>
    public string Code { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>SIGNED points: a bonus is positive (+2), a penalty is negative (-5).</summary>
    public int Points { get; set; }

    /// <summary>
    /// false for N04 (excused absence) and B01/B02 — tracked only, never scored.
    /// </summary>
    public bool CountsForScore { get; set; }

    /// <summary>
    /// null = a school-wide standard code (the default).
    /// Non-null = a code the homeroom teacher added, visible only to that class.
    /// See <see cref="ClassViolationOverride"/>.
    /// </summary>
    public int? ClassId { get; set; }

    /// <summary>Roles allowed to record this code. C10/C12/B01/B02 are homeroom-teacher only.</summary>
    public List<Role> AllowedRoles { get; set; } = new();

    /// <summary>
    /// DECISION Q5: every violation is INDIVIDUAL. The column is kept for the future
    /// and defaults to false.
    /// </summary>
    public bool IsBulkCapable { get; set; }

    /// <summary>C06 and C07 are computed by the system; nobody enters them by hand.</summary>
    public bool IsAutoComputed { get; set; }

    /// <summary>
    /// Maximum number of times this can be recorded for one student in one week.
    /// null means no limit.
    /// </summary>
    public int? MaxPerWeek { get; set; }

    // --- Remediation requirements ---

    public bool RequiresRemediation { get; set; }

    /// <summary>"Trực bổ sung 1 tuần", "Viết bản kiểm điểm"... — shown to users, stays in Vietnamese.</summary>
    public string? RemediationNote { get; set; }

    /// <summary>Number of days allowed for remediation (N08 = 3).</summary>
    public int? RemediationDays { get; set; }

    public int Ordinal { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public ViolationCategory Category { get; set; } = null!;
    public Class? Class { get; set; }
    public ICollection<ViolationRecord> Violations { get; set; } = new List<ViolationRecord>();
    public ICollection<ClassViolationOverride> Overrides { get; set; } = new List<ClassViolationOverride>();
}
