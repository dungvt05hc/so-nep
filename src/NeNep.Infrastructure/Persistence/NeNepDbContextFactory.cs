using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NeNep.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time. Reads the connection string from the
/// <c>NENEP_CONNECTION</c> environment variable, defaulting to the Postgres instance
/// in <c>docker-compose.yml</c>.
/// </summary>
public class NeNepDbContextFactory : IDesignTimeDbContextFactory<NeNepDbContext>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=nenep;Username=nenep;Password=nenep";

    public NeNepDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("NENEP_CONNECTION") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<NeNepDbContext>()
            .UseNpgsql(
                DependencyInjection.BuildDataSource(connectionString),
                DependencyInjection.ConfigureNpgsql)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NeNepDbContext(options);
    }
}
