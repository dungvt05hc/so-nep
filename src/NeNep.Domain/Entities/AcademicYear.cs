using NeNep.Domain.Abstractions;

namespace NeNep.Domain.Entities;

/// <summary>An academic year, for example "2026-2027".</summary>
public class AcademicYear : ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>"2026-2027"</summary>
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public ICollection<Term> Terms { get; set; } = new List<Term>();
    public ICollection<AcademicWeek> Weeks { get; set; } = new List<AcademicWeek>();
    public ICollection<SchoolBreak> Breaks { get; set; } = new List<SchoolBreak>();
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<ConductScore> Scores { get; set; } = new List<ConductScore>();

    /// <summary>
    /// Soft deletion. Only a year that has no term, week, break or class attached can be
    /// deleted; anything further along is history and stays.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
