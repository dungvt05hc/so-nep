using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Auditing;

/// <summary>The business meaning a feature attaches to a pending change.</summary>
public readonly record struct AuditTag(AuditAction Action, string? Summary);

/// <summary>
/// Lets a feature say what a write MEANS in business terms before it is saved.
/// <para>
/// <c>AuditSaveChangesInterceptor</c> already records every write on its own, but from
/// the change tracker alone a password reset and a name change both look like an UPDATE
/// on <c>users</c>. Tagging the entity keeps the audit trail readable without any
/// feature ever creating an <see cref="Domain.Entities.AuditLog"/> by hand — that stays
/// the interceptor's exclusive job.
/// </para>
/// </summary>
public interface IAuditScope
{
    /// <summary>
    /// Declares that the pending change on <paramref name="entity"/> is
    /// <paramref name="action"/>. The optional summary is shown in the audit screen, so
    /// it is written in Vietnamese for the reader.
    /// </summary>
    void Tag(object entity, AuditAction action, string? summary = null);

    bool TryGet(object entity, out AuditTag tag);
}

/// <summary>
/// Per-request implementation. Entities are keyed by reference, so tagging one instance
/// never leaks onto another row of the same table.
/// </summary>
public sealed class AuditScope : IAuditScope
{
    private readonly Dictionary<object, AuditTag> _tags = new(ReferenceEqualityComparer.Instance);

    public void Tag(object entity, AuditAction action, string? summary = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _tags[entity] = new AuditTag(action, summary);
    }

    public bool TryGet(object entity, out AuditTag tag) => _tags.TryGetValue(entity, out tag);
}
