using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NeNep.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Replacement for Prisma's <c>@updatedAt</c>, which EF Core has no built-in equivalent for.
/// Every property annotated with <see cref="NeNepAnnotations.UpdatedAt"/> in an
/// <c>IEntityTypeConfiguration</c> is stamped with the current instant whenever the
/// entity is inserted or updated.
/// <para>
/// The time comes from the injected <see cref="TimeProvider"/>, never from
/// <c>DateTime.Now</c>, so tests can move time forward.
/// </para>
/// </summary>
public sealed class TimestampInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public TimestampInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Metadata.GetProperties())
            {
                if (property.FindAnnotation(NeNepAnnotations.UpdatedAt) is null)
                {
                    continue;
                }

                PropertyEntry propertyEntry = entry.Property(property.Name);
                propertyEntry.CurrentValue = now;
            }
        }
    }
}
