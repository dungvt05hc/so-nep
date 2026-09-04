namespace NeNep.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        Login.Map(group);
        RefreshSession.Map(group);
        Logout.Map(group);
        ChangePassword.Map(group);
        GetProfile.Map(group);

        return app;
    }
}
