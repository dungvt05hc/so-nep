using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NeNep.Domain.Abstractions;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;

namespace NeNep.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes an <c>audit_logs</c> row for EVERY write, instead of relying on each service
/// to remember to do it. This is the only place allowed to create an <see cref="AuditLog"/>.
/// <para>
/// The interceptor also enforces the APPEND-ONLY rule for <c>audit_logs</c>: no updates,
/// no deletes, no soft deletes.
/// </para>
/// <para>
/// SKELETON (Phase 0): CREATE, UPDATE and DELETE are covered, and soft deletes are
/// recognised as deletes. The business-specific actions (APPROVE, LOCK_WEEK, PUBLISH,
/// ISSUE_PARENT_CODE and so on) will be added as their features are built.
/// </para>
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IAuditContext _auditContext;
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(IAuditContext auditContext, TimeProvider timeProvider)
    {
        _auditContext = auditContext;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Audit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Audit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Audit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var now = _timeProvider.GetUtcNow();
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog)
            {
                GuardAppendOnly(entry);
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            logs.Add(BuildLog(entry, now));
        }

        if (logs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(logs);
        }
    }

    /// <summary>
    /// <c>audit_logs</c> is append-only. Updating or deleting an audit row is a
    /// programming error rather than a business scenario, so it is blocked here.
    /// </summary>
    private static void GuardAppendOnly(EntityEntry entry)
    {
        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            throw new InvalidOperationException(
                "audit_logs is append-only: audit rows must never be updated or deleted.");
        }
    }

    private AuditLog BuildLog(EntityEntry entry, DateTimeOffset now)
    {
        var action = ResolveAction(entry);

        return new AuditLog
        {
            ActorId = _auditContext.ActorId,
            ActorRole = _auditContext.ActorRole,
            Action = action,
            Entity = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
            EntityId = ResolveEntityId(entry),
            ClassId = ResolveClassId(entry),
            BeforeJson = entry.State == EntityState.Added ? null : Snapshot(entry, original: true),
            AfterJson = entry.State == EntityState.Deleted ? null : Snapshot(entry, original: false),
            Ip = _auditContext.Ip,
            UserAgent = _auditContext.UserAgent,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// A soft delete (setting <see cref="ISoftDeletable.DeletedAt"/>) is technically an
    /// UPDATE but is a DELETE in business terms, and the audit trail must record the
    /// business meaning.
    /// </summary>
    private static AuditAction ResolveAction(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => AuditAction.CREATE,
        EntityState.Deleted => AuditAction.DELETE,
        EntityState.Modified when IsSoftDelete(entry) => AuditAction.DELETE,
        _ => AuditAction.UPDATE,
    };

    private static bool IsSoftDelete(EntityEntry entry)
    {
        if (entry.Entity is not ISoftDeletable)
        {
            return false;
        }

        var property = entry.Properties
            .FirstOrDefault(p => p.Metadata.Name == nameof(ISoftDeletable.DeletedAt));

        return property is { IsModified: true, OriginalValue: null, CurrentValue: not null };
    }

    private static string? ResolveEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();

        if (key is null)
        {
            return null;
        }

        var values = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);

        return string.Join(':', values);
    }

    /// <summary>
    /// Records <c>class_id</c> when the entity has that column, so the school board can
    /// filter the audit trail by class without an extra join.
    /// </summary>
    private static int? ResolveClassId(EntityEntry entry)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ClassId");

        return property?.CurrentValue as int?;
    }

    private static JsonDocument? Snapshot(EntityEntry entry, bool original)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            // For an UPDATE only record what actually changed, so the trail stays readable.
            if (entry.State == EntityState.Modified && !property.IsModified)
            {
                continue;
            }

            values[property.Metadata.Name] = original ? property.OriginalValue : property.CurrentValue;
        }

        if (values.Count == 0)
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(values, JsonOptions);
    }
}
