using NeNep.Domain.Abstractions;

namespace NeNep.Domain.Entities;

/// <summary>A class belonging to one specific academic year.</summary>
public class Class : ISoftDeletable
{
    public int Id { get; set; }

    public int GradeId { get; set; }

    public int YearId { get; set; }

    /// <summary>"9/1"</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? HomeroomTeacherId { get; set; }

    public Grade Grade { get; set; } = null!;
    public AcademicYear Year { get; set; } = null!;
    public User? HomeroomTeacher { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<ClassOfficer> Officers { get; set; } = new List<ClassOfficer>();
    public ICollection<ViolationRecord> Violations { get; set; } = new List<ViolationRecord>();
    public ICollection<WeeklyReport> Reports { get; set; } = new List<WeeklyReport>();
    public ICollection<ConductScore> Scores { get; set; } = new List<ConductScore>();

    /// <summary>Class-specific codes the homeroom teacher added for their own class.</summary>
    public ICollection<ViolationType> ViolationTypes { get; set; } = new List<ViolationType>();

    public ICollection<ClassViolationOverride> Overrides { get; set; } = new List<ClassViolationOverride>();
    public ICollection<ConductAlert> Alerts { get; set; } = new List<ConductAlert>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    /// <summary>
    /// Soft deletion, for a class created by mistake. A class that already has students
    /// or records is never deleted: everything downstream points at it.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
