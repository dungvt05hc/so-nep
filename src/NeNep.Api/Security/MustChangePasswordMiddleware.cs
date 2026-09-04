using NeNep.Api.Common;

namespace NeNep.Api.Security;

/// <summary>
/// Marks an endpoint that stays reachable while the account still owes a password
/// change: signing out, reading your own profile, and the change itself.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowMustChangePasswordAttribute : Attribute;

/// <summary>
/// Blocks every other endpoint until an account created or reset by a teacher has chosen
/// its own password, so a temporary password printed on paper is never left in use.
/// </summary>
public sealed class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || !currentUser.MustChangePassword)
        {
            await _next(context);
            return;
        }

        var allowed = context.GetEndpoint()?.Metadata
            .GetMetadata<AllowMustChangePasswordAttribute>() is not null;

        if (allowed)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await context.Response.WriteAsJsonAsync(
            new ApiError(
                "MUST_CHANGE_PASSWORD",
                "Bạn cần đổi mật khẩu trước khi sử dụng hệ thống."),
            context.RequestAborted);
    }
}

public static class MustChangePasswordExtensions
{
    public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder app) =>
        app.UseMiddleware<MustChangePasswordMiddleware>();

    /// <summary>Keeps this endpoint reachable while a password change is pending.</summary>
    public static TBuilder AllowMustChangePassword<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AllowMustChangePasswordAttribute());
}
