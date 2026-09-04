using System.Security.Claims;
using NeNep.Domain.Enums;

namespace NeNep.Api.Security;

/// <summary>Who is calling, read from the validated access token.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    int UserId { get; }

    string Username { get; }

    /// <summary>Identifier of the <c>sessions</c> row this token belongs to.</summary>
    string SessionId { get; }

    IReadOnlyCollection<Role> Roles { get; }

    bool MustChangePassword { get; }

    bool IsInRole(Role role);
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    private ClaimsPrincipal? _cachedFor;
    private IReadOnlyCollection<Role> _cachedRoles = [];

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <summary>
    /// Read on every access, never cached in the constructor. This service can be built
    /// before authentication has run — the JWT event handler resolves a DbContext, which
    /// drags the audit context and this type in with it — and at that moment
    /// <c>HttpContext.User</c> is still the anonymous principal that authentication is
    /// about to REPLACE. Holding on to it would leave every later caller anonymous.
    /// </summary>
    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId => int.TryParse(Find(TokenClaims.UserId), out var id)
        ? id
        : throw new InvalidOperationException("The access token carries no user id.");

    public string Username => Find(TokenClaims.Username) ?? string.Empty;

    public string SessionId => Find(TokenClaims.SessionId)
        ?? throw new InvalidOperationException("The access token carries no session id.");

    public IReadOnlyCollection<Role> Roles
    {
        get
        {
            var principal = Principal;

            if (!ReferenceEquals(principal, _cachedFor))
            {
                _cachedFor = principal;
                _cachedRoles = ReadRoles(principal);
            }

            return _cachedRoles;
        }
    }

    public bool MustChangePassword => Find(TokenClaims.MustChangePassword) == "1";

    public bool IsInRole(Role role) => Roles.Contains(role);

    private string? Find(string type) => Principal?.FindFirst(type)?.Value;

    private static IReadOnlyCollection<Role> ReadRoles(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return [];
        }

        return principal.FindAll(TokenClaims.Role)
            .Select(c => Enum.TryParse<Role>(c.Value, out var role) ? role : (Role?)null)
            .Where(r => r is not null)
            .Select(r => r!.Value)
            .Distinct()
            .ToArray();
    }
}

/// <summary>
/// Claim names used in the access token. Kept short because the token travels on every
/// request from a phone on a mobile connection.
/// </summary>
public static class TokenClaims
{
    public const string UserId = "sub";
    public const string SessionId = "sid";
    public const string Username = "name";
    public const string FullName = "fullname";
    public const string Role = "role";

    /// <summary>"1" while the account still has to choose its own password.</summary>
    public const string MustChangePassword = "mcp";
}
