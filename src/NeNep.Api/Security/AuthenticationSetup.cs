using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NeNep.Api.Common;

namespace NeNep.Api.Security;

public static class AuthenticationSetup
{
    /// <summary>
    /// Registers JWT bearer authentication, the role policies, and everything an endpoint
    /// needs in order to know who is calling and which classes they own.
    /// </summary>
    public static IServiceCollection AddNeNepAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SigningKey) && Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "Auth:SigningKey is missing or shorter than 32 bytes.")
            .Validate(o => o.AccessTokenMinutes > 0, "Auth:AccessTokenMinutes must be positive.")
            .Validate(o => o.RefreshTokenDays > 0, "Auth:RefreshTokenDays must be positive.")
            .Validate(o => o.MaxFailedAttempts > 0, "Auth:MaxFailedAttempts must be positive.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IClassAccessGuard, ClassAccessGuard>();
        services.AddScoped<ISessionValidator, SessionValidator>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddAuthorizationBuilder().AddNeNepPolicies();

        return services;
    }

    /// <summary>
    /// Written as an <see cref="IConfigureOptions{TOptions}"/> so the signing key and the
    /// <see cref="TimeProvider"/> come from DI rather than being captured at start-up.
    /// </summary>
    private sealed class ConfigureJwtBearer : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly AuthOptions _auth;
        private readonly TimeProvider _timeProvider;

        public ConfigureJwtBearer(IOptions<AuthOptions> auth, TimeProvider timeProvider)
        {
            _auth = auth.Value;
            _timeProvider = timeProvider;
        }

        public void Configure(string? name, JwtBearerOptions options) => Configure(options);

        public void Configure(JwtBearerOptions options)
        {
            // Keep the claim names exactly as issued; the default mapping would rename them.
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _auth.Issuer,
                ValidateAudience = true,
                ValidAudience = _auth.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_auth.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = TokenClaims.Username,
                RoleClaimType = TokenClaims.Role,

                // Never DateTime.UtcNow: tests move the clock forward to watch a token expire.
                LifetimeValidator = (notBefore, expires, _, _) =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;

                    return (notBefore is null || notBefore <= now) && (expires is null || expires > now);
                },
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;

                    var sessionId = principal?.FindFirst(TokenClaims.SessionId)?.Value;
                    var userId = principal?.FindFirst(TokenClaims.UserId)?.Value;

                    if (sessionId is null || !int.TryParse(userId, out var id))
                    {
                        context.Fail("The access token carries no session.");
                        return;
                    }

                    var validator = context.HttpContext.RequestServices
                        .GetRequiredService<ISessionValidator>();

                    if (!await validator.IsAliveAsync(sessionId, id, context.HttpContext.RequestAborted))
                    {
                        // Revoked account, revoked session, or an expired session row.
                        context.Fail("This session is no longer valid.");
                    }
                },

                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    await context.Response.WriteAsJsonAsync(
                        new ApiError("UNAUTHORIZED", "Phiên đăng nhập đã hết hiệu lực. Vui lòng đăng nhập lại."),
                        context.HttpContext.RequestAborted);
                },

                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                    await context.Response.WriteAsJsonAsync(
                        new ApiError("FORBIDDEN", "Bạn không có quyền thực hiện thao tác này."),
                        context.HttpContext.RequestAborted);
                },
            };
        }
    }
}
