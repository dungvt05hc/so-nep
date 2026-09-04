using System.Text.Json;
using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// The audit trail. Append-only: never updated, never deleted, not even soft-deleted.
/// Rows are produced automatically by <c>AuditSaveChangesInterceptor</c>.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public int? ActorId { get; set; }

    public Role? ActorRole { get; set; }

    public AuditAction Action { get; set; }

    public string Entity { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public int? ClassId { get; set; }

    public JsonDocument? BeforeJson { get; set; }

    public JsonDocument? AfterJson { get; set; }

    public string? Summary { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User? Actor { get; set; }
    public Class? Class { get; set; }
}
