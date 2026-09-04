using System.Text.Json.Serialization;
using FluentValidation;
using NeNep.Api.Common;
using NeNep.Api.Features.AcademicYears;
using NeNep.Api.Features.Auth;
using NeNep.Api.Features.Classes;
using NeNep.Api.Features.Grades;
using NeNep.Api.Features.Setup;
using NeNep.Api.Features.Students;
using NeNep.Api.Security;
using NeNep.Infrastructure;
using NeNep.Infrastructure.Auditing;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing connection string ConnectionStrings:Default in configuration.");

// Registered before the infrastructure, whose TryAdd would otherwise install the
// no-actor NullAuditContext used by background jobs.
builder.Services.AddScoped<IAuditContext, HttpAuditContext>();

builder.Services.AddNeNepInfrastructure(connectionString);
builder.Services.AddNeNepAuth(builder.Configuration);

builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

// Enums travel as their labels ("GVCN", "LOCKED"), never as ordinals: the labels are the
// same strings stored in the PostgreSQL enum types, and the front end generates its types
// from this API.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<ApiErrorHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Runs after authentication so it can see who the caller is, and after routing so it can
// tell whether the endpoint is one of the few allowed before the password change.
app.UseMustChangePassword();

app.MapAuthEndpoints();
app.MapAcademicYearEndpoints();
app.MapGradeEndpoints();
app.MapClassEndpoints();
app.MapStudentEndpoints();

await app.EnsureBootstrapAdminAsync();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

/// <summary>Anchor type for <c>WebApplicationFactory</c> in integration tests.</summary>
public partial class Program;
