using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Common;

/// <summary>
/// Reads the single <c>school_settings</c> row.
/// <para>
/// PRINCIPLE 1: every number defined by the school regulations — the base score, the
/// classification thresholds, the lock hour, the grace period — lives in that row.
/// A handler that needs one asks here; it never writes the number down.
/// </para>
/// </summary>
public interface ISchoolSettings
{
    ValueTask<SchoolSetting> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Resolved once per request, since several handlers ask for it in one call.</summary>
public sealed class SchoolSettingsAccessor : ISchoolSettings
{
    private readonly NeNepDbContext _db;

    private SchoolSetting? _cached;

    public SchoolSettingsAccessor(NeNepDbContext db)
    {
        _db = db;
    }

    public async ValueTask<SchoolSetting> GetAsync(CancellationToken cancellationToken = default)
    {
        return _cached ??= await _db.SchoolSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "The school_settings row is missing. It is created by CatalogSeeder on start-up.");
    }
}
