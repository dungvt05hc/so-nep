using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AcademicYears;

/// <summary>
/// Deletes an academic year created by mistake.
/// <para>
/// Soft delete only, and only while the year is still empty: no term, no week, no break,
/// no class. Once the calendar is laid out, deleting the year would leave weeks and
/// records pointing at something the school can no longer see, so the request is refused
/// and the administrator has to take the pieces apart first.
/// </para>
/// </summary>
public static class DeleteAcademicYear
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapDelete("/{yearId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteAcademicYear")
            .WithSummary("Xoá năm học");

    private static async Task<IResult> HandleAsync(
        int yearId,
        NeNepDbContext db,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");

        if (year.IsCurrent)
        {
            throw new ConflictException(
                "Không thể xoá năm học đang áp dụng. Hãy chuyển năm học hiện hành trước.",
                "YEAR_IS_CURRENT");
        }

        var blockers = new List<string>();

        if (await db.Terms.AnyAsync(t => t.YearId == yearId, cancellationToken))
        {
            blockers.Add("học kỳ");
        }

        if (await db.AcademicWeeks.AnyAsync(w => w.YearId == yearId, cancellationToken))
        {
            blockers.Add("tuần học");
        }

        if (await db.SchoolBreaks.AnyAsync(b => b.YearId == yearId, cancellationToken))
        {
            blockers.Add("kỳ nghỉ");
        }

        if (await db.Classes.IgnoreQueryFilters().AnyAsync(c => c.YearId == yearId, cancellationToken))
        {
            blockers.Add("lớp");
        }

        if (blockers.Count > 0)
        {
            throw new ConflictException(
                $"Năm học đã có {string.Join(", ", blockers)} nên không xoá được.",
                "YEAR_IN_USE");
        }

        year.DeletedAt = timeProvider.GetUtcNow();

        auditScope.Tag(year, AuditAction.DELETE, $"Xoá năm học {year.Name}.");

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
