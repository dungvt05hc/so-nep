using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;
using NeNep.Infrastructure.Persistence.Interceptors;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace NeNep.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="NeNepDbContext"/>, a data source with every enum mapped,
    /// and the two mandatory interceptors (audit and timestamp).
    /// </summary>
    public static IServiceCollection AddNeNepInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton(_ => BuildDataSource(connectionString));

        // Never use DateTime.Now: every timestamp goes through TimeProvider so tests
        // can move time forward.
        services.TryAddSingleton(TimeProvider.System);

        // The API layer replaces this with an implementation backed by HttpContext.
        services.TryAddScoped<IAuditContext, NullAuditContext>();

        services.AddScoped<TimestampInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<NeNepDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), ConfigureNpgsql)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<TimestampInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }

    /// <summary>
    /// Declares the enums at the EF layer. Without this step <c>HasPostgresEnum</c> still
    /// creates the types in the database, but the columns come out as <c>integer</c> —
    /// see <see cref="NeNepEnums"/>.
    /// </summary>
    public static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder options)
    {
        foreach (var enumType in NeNepEnums.All)
        {
            options.MapEnum(enumType, nameTranslator: NeNepEnumNameTranslator.Instance);
        }
    }

    /// <summary>Declares the enums at the ADO.NET layer — see <see cref="NeNepEnums"/>.</summary>
    public static NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);

        foreach (var enumType in NeNepEnums.All)
        {
            builder.MapEnum(enumType, nameTranslator: NeNepEnumNameTranslator.Instance);
        }

        return builder.Build();
    }
}
