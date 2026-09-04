namespace NeNep.Domain.Abstractions;

/// <summary>
/// Marks an entity that uses soft deletion. There is no <c>DELETE FROM</c> on any
/// business table: deleting only sets <see cref="DeletedAt"/>, and a global query
/// filter in <c>NeNepDbContext</c> removes deleted rows from every query.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Soft-deletion timestamp. <c>null</c> means the row is still active.</summary>
    DateTimeOffset? DeletedAt { get; set; }
}
