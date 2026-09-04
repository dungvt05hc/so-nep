namespace NeNep.Domain.Entities;

/// <summary>
/// Student × Class × Academic year. Required so that data from different years
/// never gets mixed together.
/// </summary>
public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public int YearId { get; set; }

    /// <summary>Position in the class register.</summary>
    public int? OrderNo { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? LeftAt { get; set; }

    public string? LeaveNote { get; set; }

    public Student Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public AcademicYear Year { get; set; } = null!;
}
