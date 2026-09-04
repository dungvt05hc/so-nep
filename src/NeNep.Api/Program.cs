using NeNep.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing connection string ConnectionStrings:Default in configuration.");

builder.Services.AddNeNepInfrastructure(connectionString);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Business endpoints are grouped by feature under Features/ and will be registered
// here as each feature is built (Phase 1 onwards).

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

/// <summary>Anchor type for <c>WebApplicationFactory</c> in integration tests.</summary>
public partial class Program;
