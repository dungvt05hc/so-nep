using NeNep.Domain.Abstractions;

namespace NeNep.Domain.Entities;

/// <summary>
/// A student. The identity persists across academic years — see <see cref="Enrollment"/>.
/// </summary>
public class Student : ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>The student code used by the school.</summary>
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Note { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<ClassOfficer> OfficerRoles { get; set; } = new List<ClassOfficer>();
    public ICollection<ViolationRecord> Violations { get; set; } = new List<ViolationRecord>();
    public ICollection<ConductScore> Scores { get; set; } = new List<ConductScore>();
    public ICollection<ConductAlert> Alerts { get; set; } = new List<ConductAlert>();
    public ICollection<ConductAdjustment> Adjustments { get; set; } = new List<ConductAdjustment>();
    public ICollection<ParentAccessCode> AccessCodes { get; set; } = new List<ParentAccessCode>();

    public User? Account { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
