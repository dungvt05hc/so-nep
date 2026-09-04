using NeNep.Domain.Authorization;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;

namespace NeNep.Api.Security;

/// <summary>
/// Fills the audit trail from the current HTTP request, so
/// <c>AuditSaveChangesInterceptor</c> can record who did what, from where.
/// </summary>
public sealed class HttpAuditContext : IAuditContext
{
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _accessor;

    public HttpAuditContext(ICurrentUser currentUser, IHttpContextAccessor accessor)
    {
        _currentUser = currentUser;
        _accessor = accessor;
    }

    public int? ActorId => _currentUser.IsAuthenticated ? _currentUser.UserId : null;

    /// <summary>
    /// The most privileged role held, because that is the authority the action was
    /// carried out under.
    /// </summary>
    public Role? ActorRole
    {
        get
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Roles.Count == 0)
            {
                return null;
            }

            if (_currentUser.IsInRole(Role.ADMIN))
            {
                return Role.ADMIN;
            }

            if (_currentUser.IsInRole(Role.GVCN))
            {
                return Role.GVCN;
            }

            if (_currentUser.IsInRole(Role.BGH))
            {
                return Role.BGH;
            }

            return _currentUser.Roles
                .Where(RoleGroups.IsClassOfficer)
                .Select(r => (Role?)r)
                .FirstOrDefault();
        }
    }

    public string? Ip => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
