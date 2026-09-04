using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeNep.Infrastructure;
using NeNep.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API against a real PostgreSQL in a container. The enum types, the
/// snake_case naming and the soft-delete filters are exactly what makes this project
/// tricky, so an in-memory provider would test the wrong thing.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SigningKey = "integration-test-signing-key-0123456789";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("nenep")
        .WithUsername("nenep")
        .WithPassword("nenep")
        .Build();

    /// <summary>Test clock, injected in place of <see cref="TimeProvider.System"/>.</summary>
    public TestTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 7, 1, 0, 0, TimeSpan.Zero));

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Migrations run on a context built by hand, before the application starts: the
        // application must never migrate a database on its own.
        await using var db = CreateDbContext();

        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>A context on the same database, for arranging data and checking results.</summary>
    public NeNepDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NeNepDbContext>()
            .UseNpgsql(
                DependencyInjection.BuildDataSource(ConnectionString),
                DependencyInjection.ConfigureNpgsql)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NeNepDbContext(options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Auth:SigningKey", SigningKey);

        // Left empty on purpose: the bootstrap administrator must not run in tests.
        builder.UseSetting("Bootstrap:AdminUsername", string.Empty);
        builder.UseSetting("Bootstrap:AdminPassword", string.Empty);

        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Time));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
