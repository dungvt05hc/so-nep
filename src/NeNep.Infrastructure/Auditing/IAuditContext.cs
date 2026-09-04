using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Auditing;

/// <summary>
/// Information about who is performing the current operation, so that
/// <c>AuditSaveChangesInterceptor</c> can fill in <c>audit_logs</c>. The API layer
/// implements this by reading from HttpContext; background jobs and migrations use
/// <see cref="NullAuditContext"/>.
/// </summary>
public interface IAuditContext
{
    int? ActorId { get; }

    Role? ActorRole { get; }

    string? Ip { get; }

    string? UserAgent { get; }
}

/// <summary>No user at all — used by background jobs, seeding and design-time tooling.</summary>
public sealed class NullAuditContext : IAuditContext
{
    public int? ActorId => null;

    public Role? ActorRole => null;

    public string? Ip => null;

    public string? UserAgent => null;
}
