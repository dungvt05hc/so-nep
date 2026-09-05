using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Domain.Authorization;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Security;

/// <summary>
/// Exactly which classes the caller may see and change, resolved from the database and
/// never from anything the client sent.
/// </summary>
public sealed record ClassScope(
    bool SchoolWideRead,
    bool SchoolWideWrite,
    IReadOnlySet<int> ReadableClassIds,
    IReadOnlySet<int> WritableClassIds,
    IReadOnlySet<Role> SchoolWideRoles,
    IReadOnlyDictionary<int, IReadOnlySet<Role>> RolesByClass)
{
    public bool CanRead(int classId) => SchoolWideRead || ReadableClassIds.Contains(classId);

    public bool CanWrite(int classId) => SchoolWideWrite || WritableClassIds.Contains(classId);

    /// <summary>
    /// The roles the caller actually holds INSIDE this class, school-wide roles included.
    /// <para>
    /// A person can be the homeroom teacher of 9/1 and nothing at all in 9/2, so
    /// "is this caller a GVCN" is never a question that can be answered without naming
    /// the class — which is exactly what the catalog's <c>allowed_roles</c> check needs.
    /// </para>
    /// </summary>
    public IReadOnlySet<Role> RolesIn(int classId)
    {
        if (!RolesByClass.TryGetValue(classId, out var roles))
        {
            return SchoolWideRoles;
        }

        var combined = new HashSet<Role>(roles);
        combined.UnionWith(SchoolWideRoles);

        return combined;
    }

    /// <summary>
    /// The class ids a listing must be narrowed to, or <c>null</c> when the caller sees
    /// the whole school.
    /// </summary>
    public int[]? ReadFilter() => SchoolWideRead ? null : [.. ReadableClassIds];
}

/// <summary>
/// The one gate every endpoint that touches class data has to pass through.
/// <para>
/// The rule is absolute: a <c>classId</c> arriving from the client is a REQUEST, never a
/// permission. The homeroom teacher of 9/1 asking for 9/2 gets a 403, whichever endpoint
/// they use.
/// </para>
/// </summary>
public interface IClassAccessGuard
{
    ValueTask<ClassScope> GetScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="ForbiddenException"/> unless the caller may read the class.</summary>
    ValueTask EnsureCanReadAsync(int classId, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="ForbiddenException"/> unless the caller may change the class.</summary>
    ValueTask EnsureCanWriteAsync(int classId, CancellationToken cancellationToken = default);
}

public sealed class ClassAccessGuard : IClassAccessGuard
{
    private const string Denied = "Bạn không có quyền truy cập dữ liệu của lớp này.";

    private readonly NeNepDbContext _db;
    private readonly ICurrentUser _currentUser;

    private ClassScope? _scope;

    public ClassAccessGuard(NeNepDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ClassScope> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        // Resolved once per request: the guard is scoped, and every endpoint calls it.
        return _scope ??= await ResolveAsync(cancellationToken);
    }

    public async ValueTask EnsureCanReadAsync(int classId, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(cancellationToken);

        if (!scope.CanRead(classId))
        {
            throw new ForbiddenException(Denied, "CLASS_ACCESS_DENIED");
        }
    }

    public async ValueTask EnsureCanWriteAsync(int classId, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(cancellationToken);

        if (!scope.CanWrite(classId))
        {
            throw new ForbiddenException(Denied, "CLASS_ACCESS_DENIED");
        }
    }

    private static HashSet<Role> RolesOf(Dictionary<int, HashSet<Role>> byClass, int classId)
    {
        if (!byClass.TryGetValue(classId, out var roles))
        {
            roles = [];
            byClass[classId] = roles;
        }

        return roles;
    }

    private async Task<ClassScope> ResolveAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return new ClassScope(
                false,
                false,
                new HashSet<int>(),
                new HashSet<int>(),
                new HashSet<Role>(),
                new Dictionary<int, IReadOnlySet<Role>>());
        }

        var userId = _currentUser.UserId;

        var roles = await _db.UserRoles
            .Where(r => r.UserId == userId)
            .Select(r => new { r.Role, r.ClassId })
            .ToListAsync(cancellationToken);

        var schoolWideRead = roles.Any(r => RoleGroups.IsSchoolWide(r.Role));
        var schoolWideWrite = roles.Any(r => r.Role == Role.ADMIN);

        var schoolWideRoles = roles
            .Where(r => RoleGroups.IsSchoolWide(r.Role))
            .Select(r => r.Role)
            .ToHashSet();

        var readable = new HashSet<int>();
        var writable = new HashSet<int>();
        var byClass = new Dictionary<int, HashSet<Role>>();

        foreach (var role in roles.Where(r => RoleGroups.IsClassScoped(r.Role) && r.ClassId is not null))
        {
            readable.Add(role.ClassId!.Value);

            // Class officers report into their own class; the homeroom teacher owns it.
            writable.Add(role.ClassId.Value);

            RolesOf(byClass, role.ClassId.Value).Add(role.Role);
        }

        if (roles.Any(r => r.Role == Role.GVCN))
        {
            // The homeroom assignment on the class itself is the authoritative link, so a
            // teacher who takes over a class mid-year gets access without a role row.
            var homeroom = await _db.Classes
                .Where(c => c.HomeroomTeacherId == userId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            readable.UnionWith(homeroom);
            writable.UnionWith(homeroom);

            foreach (var classId in homeroom)
            {
                RolesOf(byClass, classId).Add(Role.GVCN);
            }
        }

        return new ClassScope(
            schoolWideRead,
            schoolWideWrite,
            readable,
            writable,
            schoolWideRoles,
            byClass.ToDictionary(e => e.Key, e => (IReadOnlySet<Role>)e.Value));
    }
}
