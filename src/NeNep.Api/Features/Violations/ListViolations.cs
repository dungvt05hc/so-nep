using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Domain.Authorization;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The review queue and the class register behind one endpoint, narrowed by the filters
/// the two screens need.
/// <para>
/// A class officer only ever sees the records they entered themselves ("Bản ghi của
/// tôi"): they are pupils, and the conduct file of their classmates is not theirs to
/// browse. The homeroom teacher, the administration and the school board see the class.
/// </para>
/// </summary>
public static class ListViolations
{
    private const int MaxPageSize = 200;

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/violations", HandleAsync)
            .RequireAuthorization()
            .WithName("ListViolations")
            .WithSummary("Danh sách bản ghi của lớp");

    private static async Task<ViolationPageResponse> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ICurrentUser currentUser,
        CancellationToken cancellationToken,
        int? weekId = null,
        ViolationStatus? status = null,
        int? studentId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool? flagged = null,
        bool mine = false,
        int page = 1,
        int pageSize = 50)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        var scope = await guard.GetScopeAsync(cancellationToken);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.ViolationRecords
            .AsNoTracking()
            .Where(r => r.ClassId == classId);

        var roles = scope.RolesIn(classId);
        var officerOnly = roles.Any(RoleGroups.IsClassOfficer)
            && !roles.Contains(Role.GVCN)
            && !roles.Contains(Role.ADMIN)
            && !roles.Contains(Role.BGH);

        if (officerOnly || mine)
        {
            var userId = currentUser.UserId;

            query = query.Where(r => r.ReportedById == userId);
        }

        if (weekId is not null)
        {
            query = query.Where(r => r.WeekId == weekId);
        }

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        if (studentId is not null)
        {
            query = query.Where(r => r.StudentId == studentId);
        }

        if (from is not null)
        {
            query = query.Where(r => r.OccurredDate >= from);
        }

        if (to is not null)
        {
            query = query.Where(r => r.OccurredDate <= to);
        }

        if (flagged is not null)
        {
            query = query.Where(r => r.IsFlagged == flagged);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            // Flagged records first: they are the ones the homeroom teacher must look at.
            .OrderByDescending(r => r.IsFlagged)
            .ThenByDescending(r => r.OccurredDate)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ViolationProjection.ToResponse)
            .ToListAsync(cancellationToken);

        return new ViolationPageResponse(items, total, page, pageSize);
    }
}
