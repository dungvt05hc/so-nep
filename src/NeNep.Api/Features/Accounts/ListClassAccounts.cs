using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Accounts;

/// <summary>Lists the class-officer accounts of one class.</summary>
public static class ListClassAccounts
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/accounts", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("ListClassAccounts")
            .WithSummary("Danh sách tài khoản cán bộ lớp");

    private static async Task<IReadOnlyList<AccountResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        return await AccountQueries.ProjectOfficers(db, classId)
            .AsNoTracking()
            .OrderBy(a => a.Role)
            .ThenBy(a => a.Username)
            .ToListAsync(cancellationToken);
    }
}
