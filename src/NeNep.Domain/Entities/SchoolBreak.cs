namespace NeNep.Domain.Entities;

/// <summary>
/// A SCHOOL BREAK — public holidays, Tết, or unscheduled closures (storm, epidemic).
/// The school usually cannot plan these in advance, so this data is entered mid-year.
/// Adding a break SHIFTS the dates of every unlocked academic week forward while
/// keeping their week numbers. Locked weeks are never touched.
/// </summary>
public class SchoolBreak
{
    public int Id { get; set; }

    public int YearId { get; set; }

    /// <summary>"Nghỉ Tết Đinh Mùi", "Nghỉ bão số 5" — shown to users, stays in Vietnamese.</summary>
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>
    /// false = still provisional, with no official announcement from the school or
    /// the education department yet.
    /// </summary>
    public bool IsConfirmed { get; set; }

    /// <summary>How many academic weeks this break shifted. Recorded so it can be undone.</summary>
    public int ShiftedWeeks { get; set; }

    public DateTimeOffset? AppliedAt { get; set; }

    public int? AppliedById { get; set; }

    public string? Note { get; set; }

    public AcademicYear Year { get; set; } = null!;
}
