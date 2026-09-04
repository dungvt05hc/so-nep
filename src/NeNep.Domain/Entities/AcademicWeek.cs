using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// AN ACADEMIC WEEK. Only weeks with actual teaching exist here, and only those
/// receive a <see cref="WeekNo"/>.
/// A public holiday or Tết break is NOT an academic week — see <see cref="SchoolBreak"/>.
/// That keeps <see cref="WeekNo"/> aligned with how the school numbers its weeks (1..35),
/// and keeps the denominator of "term score = total ÷ number of weeks" correct.
/// </summary>
public class AcademicWeek
{
    public int Id { get; set; }

    public int YearId { get; set; }

    public int TermId { get; set; }

    /// <summary>Sequence number of the week within the academic year.</summary>
    public int WeekNo { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>"Nghỉ Tết", "Tuần kiểm tra HK1"... — shown to users, so it stays in Vietnamese.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// false = teaching took place but the week does NOT count towards the term score.
    /// Used for exam weeks, weeks with only two or three sessions, or weeks with a
    /// major event. Changing this flag after the week is locked forces a recalculation
    /// of the whole class's term score.
    /// </summary>
    public bool IsCounted { get; set; }

    public string? NotCountedReason { get; set; }

    /// <summary>
    /// The originally planned date, kept for reference when the calendar is shifted
    /// by an unscheduled break.
    /// </summary>
    public DateOnly? OriginalStartDate { get; set; }

    public WeekStatus Status { get; set; }

    public DateTimeOffset? LockedAt { get; set; }

    public ICollection<ViolationRecord> Violations { get; set; } = new List<ViolationRecord>();
    public ICollection<ConductScore> Scores { get; set; } = new List<ConductScore>();
    public ICollection<WeeklyReport> Reports { get; set; } = new List<WeeklyReport>();

    public AcademicYear Year { get; set; } = null!;
    public Term Term { get; set; } = null!;
}
