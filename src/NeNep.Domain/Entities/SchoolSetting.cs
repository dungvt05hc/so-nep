namespace NeNep.Domain.Entities;

/// <summary>
/// School-wide configuration. Exactly one row exists (id = 1).
/// Every number defined by the school regulations lives here and must never be
/// hardcoded anywhere in the code.
/// </summary>
public class SchoolSetting
{
    public int Id { get; set; } = 1;

    public string SchoolName { get; set; } = string.Empty;

    public string SchoolCode { get; set; } = string.Empty;

    // --- Scoring formula ---

    /// <summary>Starting score for each week.</summary>
    public int BaseScore { get; set; }

    /// <summary>
    /// DECISION Q1: bonus points have NO cap. The column is kept in case the school
    /// changes its mind; null by default.
    /// </summary>
    public int? ScoreCap { get; set; }

    /// <summary>DECISION Q2: scores ARE allowed to go negative. null means no floor.</summary>
    public int? ScoreFloor { get; set; }

    /// <summary>
    /// DECISION Q3 + Q4: the app does NOT downgrade or upgrade conduct by itself.
    /// A downgrade is decided at a school board / homeroom teacher / parent meeting
    /// and recorded in <see cref="ConductAdjustment"/>.
    /// Only turn this flag on if the school changes that policy.
    /// </summary>
    public bool AutoApplyLevelRules { get; set; }

    /// <summary>
    /// Warn early when a student is within N occurrences of a threshold
    /// (raises an APPROACHING alert).
    /// </summary>
    public int AlertLeadCount { get; set; }

    /// <summary>
    /// DECISION Q7: whether a homeroom teacher may adjust the scoring catalog for
    /// their own class. false = they may only enable or disable codes; true = they
    /// may also change point values.
    /// Keeping this false is what makes the 62 classes comparable with each other.
    /// </summary>
    public bool AllowClassPointOverride { get; set; }

    /// <summary>
    /// DECISION Q8: C07 still awards its bonus even though each C01 was already
    /// counted individually. The system adds it automatically.
    /// </summary>
    public bool StackAutoBonus { get; set; }

    // --- Weekly lock ---

    /// <summary>0 = Sunday.</summary>
    public int LockDayOfWeek { get; set; }

    public int LockHour { get; set; }

    /// <summary>
    /// Grace period after the lock time; once it runs out, PENDING records become EXPIRED.
    /// </summary>
    public int GraceHours { get; set; }

    /// <summary>true = overdue records are discarded; false = they are auto-approved.</summary>
    public bool ExpirePendingOnLock { get; set; }

    // --- Parents ---

    public int ParentCodeLength { get; set; }

    public int ParentMaxFailedTries { get; set; }

    public int ParentLockMinutes { get; set; }

    /// <summary>Only ever shows the rank of the parent's own child.</summary>
    public bool ParentShowRank { get; set; }

    /// <summary>Set by <c>TimestampInterceptor</c>, the equivalent of Prisma's <c>@updatedAt</c>.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
