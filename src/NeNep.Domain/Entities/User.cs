using NeNep.Domain.Abstractions;

namespace NeNep.Domain.Entities;

/// <summary>
/// A login account. Parents have NO account — see <see cref="ParentAccessCode"/>.
/// </summary>
public class User : ISoftDeletable
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Force a password change on the next sign-in.</summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>A class-officer account is tied to one specific student.</summary>
    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    public int? CreatedById { get; set; }

    public User? CreatedBy { get; set; }

    public ICollection<User> Created { get; set; } = new List<User>();

    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    public ICollection<Class> HomeroomOf { get; set; } = new List<Class>();
    public ICollection<ViolationRecord> Reported { get; set; } = new List<ViolationRecord>();
    public ICollection<ViolationRecord> Reviewed { get; set; } = new List<ViolationRecord>();
    public ICollection<ViolationRecord> RemediationsOk { get; set; } = new List<ViolationRecord>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ParentAccessCode> IssuedCodes { get; set; } = new List<ParentAccessCode>();
    public ICollection<ConductAdjustment> Adjustments { get; set; } = new List<ConductAdjustment>();
    public ICollection<ClassViolationOverride> Overrides { get; set; } = new List<ClassViolationOverride>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
