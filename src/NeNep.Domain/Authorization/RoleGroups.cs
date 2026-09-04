using NeNep.Domain.Enums;

namespace NeNep.Domain.Authorization;

/// <summary>
/// Groups the six roles by the kind of access they carry. Kept next to the enum so no
/// feature has to re-state "which roles are class officers" and get it subtly wrong.
/// </summary>
public static class RoleGroups
{
    /// <summary>Roles whose scope is the whole school, so they carry no class id.</summary>
    public static readonly IReadOnlySet<Role> SchoolWide =
        new HashSet<Role> { Role.ADMIN, Role.BGH };

    /// <summary>Roles that are always tied to exactly one class.</summary>
    public static readonly IReadOnlySet<Role> ClassScoped =
        new HashSet<Role> { Role.GVCN, Role.LOP_TRUONG, Role.PHO_HOC_TAP, Role.PHO_LAO_DONG };

    /// <summary>The three student officer roles, which may only report, never approve.</summary>
    public static readonly IReadOnlySet<Role> ClassOfficers =
        new HashSet<Role> { Role.LOP_TRUONG, Role.PHO_HOC_TAP, Role.PHO_LAO_DONG };

    public static bool IsSchoolWide(Role role) => SchoolWide.Contains(role);

    public static bool IsClassScoped(Role role) => ClassScoped.Contains(role);

    public static bool IsClassOfficer(Role role) => ClassOfficers.Contains(role);
}
