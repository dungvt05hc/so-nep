using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

/// <summary>
/// Lists the classes the caller is allowed to see.
/// <para>
/// The narrowing is done from <see cref="ClassScope"/>, not from anything the client
/// asked for: a homeroom teacher sees their own classes, the school board and the
/// administrator see all of them.
/// </para>
/// </summary>
public static class ListClasses
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/", HandleAsync)
            .RequireAuthorization()
            .WithName("ListClasses")
            .WithSummary("Danh sách lớp");

    private static async Task<IReadOnlyList<ClassResponse>> HandleAsync(
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken,
        int? yearId = null,
        int? gradeId = null)
    {
        var scope = await guard.GetScopeAsync(cancellationToken);
        var readable = scope.ReadFilter();

        var query = db.Classes.AsNoTracking().AsQueryable();

        if (readable is not null)
        {
            query = query.Where(c => readable.Contains(c.Id));
        }

        if (yearId is not null)
        {
            query = query.Where(c => c.YearId == yearId);
        }

        if (gradeId is not null)
        {
            query = query.Where(c => c.GradeId == gradeId);
        }

        return await ClassQueries
            .Project(query.OrderBy(c => c.Grade.Level).ThenBy(c => c.Code))
            .ToListAsync(cancellationToken);
    }
}
