using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.ClassOfficers;

/// <summary>Lists the officer appointments of a class, past and present.</summary>
public static class ListClassOfficers
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/officers", HandleAsync)
            .RequireAuthorization()
            .WithName("ListClassOfficers")
            .WithSummary("Danh sách cán bộ lớp");

    private static async Task<IReadOnlyList<ClassOfficerResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        TimeProvider timeProvider,
        CancellationToken cancellationToken,
        bool currentOnly = false)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        var today = VietnamClock.Today(timeProvider);

        var query = db.ClassOfficers
            .AsNoTracking()
            .Where(o => o.ClassId == classId);

        if (currentOnly)
        {
            query = query.Where(o => o.ValidFrom <= today && (o.ValidTo == null || o.ValidTo >= today));
        }

        return await query
            .OrderBy(o => o.Role)
            .ThenByDescending(o => o.ValidFrom)
            .Select(o => new ClassOfficerResponse(
                o.Id,
                o.ClassId,
                o.StudentId,
                o.Student.FullName,
                o.Role,
                o.ValidFrom,
                o.ValidTo,
                o.ValidFrom <= today && (o.ValidTo == null || o.ValidTo >= today)))
            .ToListAsync(cancellationToken);
    }
}
