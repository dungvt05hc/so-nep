using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Abstractions;

namespace NeNep.Api.Common;

/// <summary>Helpers for the tables that keep deleted rows instead of removing them.</summary>
public static class SoftDelete
{
    /// <summary>
    /// Refuses a name or code that is already in use, INCLUDING by a row that was deleted.
    /// <para>
    /// The unique indexes still cover deleted rows, so a plain "already exists" would be
    /// baffling for a user staring at a list that does not contain it. The two messages
    /// keep the two situations apart.
    /// </para>
    /// </summary>
    public static async Task EnsureFreeAsync<TEntity>(
        IQueryable<TEntity> matching,
        string takenMessage,
        string takenByDeletedMessage,
        CancellationToken cancellationToken)
        where TEntity : class, ISoftDeletable
    {
        var found = await matching
            .IgnoreQueryFilters()
            .Select(e => new { e.DeletedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            return;
        }

        throw found.DeletedAt is null
            ? new ConflictException(takenMessage, "CODE_TAKEN")
            : new ConflictException(takenByDeletedMessage, "CODE_TAKEN_BY_DELETED");
    }
}
