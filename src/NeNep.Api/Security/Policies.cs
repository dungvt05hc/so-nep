using Microsoft.AspNetCore.Authorization;
using NeNep.Domain.Enums;

namespace NeNep.Api.Security;

/// <summary>
/// Authorization policies by ROLE. Which class the caller may touch is a separate
/// question, answered by <see cref="IClassAccessGuard"/> — a policy alone is never
/// enough for an endpoint that reads or writes class data.
/// </summary>
public static class Policies
{
    /// <summary>School administration: years, terms, weeks, grades, classes, settings.</summary>
    public const string Admin = "Admin";

    /// <summary>Reads the whole school: administration and the school board.</summary>
    public const string SchoolWideRead = "SchoolWideRead";

    /// <summary>Runs a class: the homeroom teacher, and the administrator on their behalf.</summary>
    public const string ManageClass = "ManageClass";

    /// <summary>Anyone who may enter a record: homeroom teacher and the three officer roles.</summary>
    public const string ReportRecords = "ReportRecords";

    public static AuthorizationBuilder AddNeNepPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(Admin, p => p.RequireRole(nameof(Role.ADMIN)))
            .AddPolicy(SchoolWideRead, p => p.RequireRole(nameof(Role.ADMIN), nameof(Role.BGH)))
            .AddPolicy(ManageClass, p => p.RequireRole(nameof(Role.ADMIN), nameof(Role.GVCN)))
            .AddPolicy(ReportRecords, p => p.RequireRole(
                nameof(Role.ADMIN),
                nameof(Role.GVCN),
                nameof(Role.LOP_TRUONG),
                nameof(Role.PHO_HOC_TAP),
                nameof(Role.PHO_LAO_DONG)));
}
