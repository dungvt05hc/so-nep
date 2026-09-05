using NeNep.Infrastructure.Persistence.Seeding;

namespace NeNep.Api.Features.Setup;

/// <summary>
/// Loads the standard catalog (classification levels, groups, the 49 codes and the 14
/// rules) into the database on start-up.
/// <para>
/// It runs on every start on purpose: the catalog is reference data agreed with the
/// school, so a deployment that changes a wording or a point value must reach the
/// database without anyone remembering to run a script. Rows are matched by code, so
/// running it again changes nothing.
/// </para>
/// </summary>
public static class SeedCatalog
{
    public static async Task SeedCatalogAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<CatalogSeeder>();

        await seeder.SeedAsync();
    }
}
