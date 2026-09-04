using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

/// <summary>Reads one class. The guard runs before the row is read, never after.</summary>
public static class GetClass
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/{classId:int}", HandleAsync)
            .RequireAuthorization()
            .WithName("GetClass")
            .WithSummary("Chi tiết lớp");

    private static async Task<ClassResponse> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        return await ClassQueries
            .Project(db.Classes.AsNoTracking().Where(c => c.Id == classId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");
    }
}
