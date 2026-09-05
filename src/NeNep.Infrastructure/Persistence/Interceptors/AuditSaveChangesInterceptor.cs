using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
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
/// CREATE, UPDATE and DELETE are derived from the change tracker, and soft deletes are
/// recognised as deletes. A feature that needs a more precise action (GRANT_ACCOUNT,
/// RESET_PASSWORD, LOCK_WEEK, APPROVE...) declares it through <see cref="IAuditScope"/>
/// instead of writing an audit row itself.
/// </para>
/// <para>
/// TWO PHASES. What changed can only be read BEFORE the save, while the change tracker
/// still holds the original values; but the id of a brand new row only exists AFTER it.
/// So the trail is captured first and written afterwards, inside the same transaction —
/// otherwise every CREATE row would point at the negative placeholder EF uses for a key
/// it has not obtained yet, and the history of a record could never be looked up.
/// </para>
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,

        // Enums are written as their LABEL ("APPROVED", "GVCN"), never as a number. The
        // trail is read by people, and an ordinal would also start meaning something else
        // the day a member is inserted into the enum.
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Properties whose value must never be copied into the audit trail. A password hash
    /// or a refresh-token hash in audit_logs would turn a read-only trail that many staff
    /// can open into a second copy of the credential store.
    /// </summary>
    private static readonly HashSet<string> RedactedProperties =
    [
        "PasswordHash",
        "RefreshHash",
        "CodeHash",
    ];

    private const string RedactedValue = "***";

    private readonly IAuditContext _auditContext;
    private readonly IAuditScope _auditScope;
    private readonly TimeProvider _timeProvider;

    private readonly List<PendingAudit> _pending = [];

    /// <summary>Set while this interceptor is saving the audit rows it just produced.</summary>
    private bool _writingTrail;

    private IDbContextTransaction? _ownedTransaction;

    public AuditSaveChangesInterceptor(
        IAuditContext auditContext,
        IAuditScope auditScope,
        TimeProvider timeProvider)
    {
        _auditContext = auditContext;
        _auditScope = auditScope;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (!_writingTrail)
        {
            Capture(eventData.Context);

            if (_pending.Count > 0 && eventData.Context!.Database.CurrentTransaction is null)
            {
                _ownedTransaction = eventData.Context.Database.BeginTransaction();
            }
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_writingTrail)
        {
            Capture(eventData.Context);

            if (_pending.Count > 0 && eventData.Context!.Database.CurrentTransaction is null)
            {
                _ownedTransaction = await eventData.Context.Database
                    .BeginTransactionAsync(cancellationToken);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_writingTrail || _pending.Count == 0 || eventData.Context is null)
        {
            return base.SavedChanges(eventData, result);
        }

        var context = eventData.Context;

        _writingTrail = true;

        try
        {
            context.Set<AuditLog>().AddRange(TakePending());

            context.SaveChanges();

            _ownedTransaction?.Commit();
        }
        finally
        {
            _writingTrail = false;

            ReleaseTransaction();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_writingTrail || _pending.Count == 0 || eventData.Context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        var context = eventData.Context;

        _writingTrail = true;

        try
        {
            context.Set<AuditLog>().AddRange(TakePending());

            await context.SaveChangesAsync(cancellationToken);

            if (_ownedTransaction is not null)
            {
                await _ownedTransaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            _writingTrail = false;

            await ReleaseTransactionAsync();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>A failed write leaves no trail, and takes the transaction down with it.</summary>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pending.Clear();

        _ownedTransaction?.Rollback();

        ReleaseTransaction();

        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pending.Clear();

        if (_ownedTransaction is not null)
        {
            await _ownedTransaction.RollbackAsync(cancellationToken);
        }

        await ReleaseTransactionAsync();

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>
    /// Reads what is about to change, while the change tracker still knows both sides of
    /// it. Nothing is written here.
    /// </summary>
    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var now = _timeProvider.GetUtcNow();

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

            _pending.Add(Describe(entry, now));
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

    private PendingAudit Describe(EntityEntry entry, DateTimeOffset now)
    {
        var tagged = _auditScope.TryGet(entry.Entity, out var tag);
        var isInsert = entry.State == EntityState.Added;

        return new PendingAudit
        {
            Entry = entry,
            IsInsert = isInsert,
            Log = new AuditLog
            {
                ActorId = _auditContext.ActorId,
                ActorRole = _auditContext.ActorRole,
                Action = tagged ? tag.Action : ResolveAction(entry),
                Summary = tagged ? tag.Summary : null,
                Entity = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,

                // A row being inserted has no id yet, and no class id either when the
                // caller set the class through a navigation. Both are read after the save.
                EntityId = isInsert ? null : ResolveEntityId(entry),
                ClassId = isInsert ? null : ResolveClassId(entry),
                BeforeJson = isInsert ? null : Snapshot(entry, original: true),
                AfterJson = entry.State == EntityState.Deleted ? null : Snapshot(entry, original: false),
                Ip = _auditContext.Ip,
                UserAgent = _auditContext.UserAgent,
                CreatedAt = now,
            },
        };
    }

    /// <summary>Fills in what only became known once the database had assigned the keys.</summary>
    private List<AuditLog> TakePending()
    {
        var logs = new List<AuditLog>(_pending.Count);

        foreach (var pending in _pending)
        {
            if (pending.IsInsert)
            {
                pending.Log.EntityId = ResolveEntityId(pending.Entry);
                pending.Log.ClassId = ResolveClassId(pending.Entry);
            }

            logs.Add(pending.Log);
        }

        _pending.Clear();

        return logs;
    }

    private void ReleaseTransaction()
    {
        _ownedTransaction?.Dispose();
        _ownedTransaction = null;
    }

    private async ValueTask ReleaseTransactionAsync()
    {
        if (_ownedTransaction is not null)
        {
            await _ownedTransaction.DisposeAsync();
            _ownedTransaction = null;
        }
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

            values[property.Metadata.Name] = RedactedProperties.Contains(property.Metadata.Name)
                ? RedactedValue
                : original ? property.OriginalValue : property.CurrentValue;
        }

        if (values.Count == 0)
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(values, JsonOptions);
    }

    /// <summary>An audit row that has been described but cannot be written yet.</summary>
    private sealed class PendingAudit
    {
        public required EntityEntry Entry { get; init; }

        public required bool IsInsert { get; init; }

        public required AuditLog Log { get; init; }
    }
}
