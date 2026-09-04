using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AcademicYears;

/// <summary>
/// Marks one year as the current one. Every screen that does not name a year explicitly
/// works against this year.
/// </summary>
public static class SetCurrentAcademicYear
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/{yearId:int}/set-current", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("SetCurrentAcademicYear")
            .WithSummary("Đặt năm học hiện hành");

    private static async Task<AcademicYearResponse> HandleAsync(
        int yearId,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears
            .Include(y => y.Terms)
            .FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");

        await CreateAcademicYear.ClearCurrentFlag(db, cancellationToken);

        year.IsCurrent = true;

        await db.SaveChangesAsync(cancellationToken);

        return CreateAcademicYear.ToResponse(year);
    }
}
