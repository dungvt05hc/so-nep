using NeNep.Api.Features.Accounts;
using NeNep.Api.Features.Catalog;
using NeNep.Api.Features.ClassOfficers;
using NeNep.Api.Features.Reports;
using NeNep.Api.Features.Students;
using NeNep.Api.Features.Violations;

namespace NeNep.Api.Features.Classes;

public static class ClassEndpoints
{
    /// <summary>
    /// Everything that hangs off a class lives under <c>/api/classes/{classId}</c> on
    /// purpose: the class id is always in the route, so no handler can forget to send it
    /// through <see cref="Security.IClassAccessGuard"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        var classes = app.MapGroup("/api/classes").WithTags("Classes");

        CreateClass.Map(classes);
        ListClasses.Map(classes);
        GetClass.Map(classes);
        UpdateClass.Map(classes);
        DeleteClass.Map(classes);

        ListStudents.Map(classes);
        CreateStudent.Map(classes);
        UpdateStudent.Map(classes);
        EndEnrollment.Map(classes);
        PreviewStudentImport.Map(classes);
        CommitStudentImport.Map(classes);

        AssignClassOfficer.Map(classes);
        ListClassOfficers.Map(classes);
        EndClassOfficer.Map(classes);

        CreateOfficerAccount.Map(classes);
        ListClassAccounts.Map(classes);
        ResetAccountPassword.Map(classes);
        SetAccountActive.Map(classes);
        RevokeAccount.Map(classes);

        ListClassViolationTypes.Map(classes);
        SetClassViolationOverride.Map(classes);

        CreateViolation.Map(classes);
        ListViolations.Map(classes);
        UpdateViolation.Map(classes);
        DeleteViolation.Map(classes);
        ApproveViolations.MapSingle(classes);
        ApproveViolations.MapBulk(classes);
        RejectViolation.Map(classes);

        ListDaysWithoutRecords.Map(classes);
        ListStudentsWithoutRecords.Map(classes);
        ListOverduePendingViolations.Map(classes);

        return app;
    }
}
