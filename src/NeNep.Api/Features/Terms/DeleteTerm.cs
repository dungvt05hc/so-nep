using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Terms;

/// <summary>
/// Deletes a term, softly, and only before its weeks have been generated. A week always
/// belongs to a term, so a term with weeks cannot leave without taking the timetable and
/// every score computed against it.
/// </summary>
public static class DeleteTerm
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapDelete("/{termId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteTerm")
            .WithSummary("Xoá học kỳ");

    private static async Task<IResult> HandleAsync(
        int termId,
        NeNepDbContext db,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == termId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy học kỳ.");

        if (await db.AcademicWeeks.AnyAsync(w => w.TermId == termId, cancellationToken))
        {
            throw new ConflictException(
                "Học kỳ đã có tuần học nên không xoá được.",
                "TERM_IN_USE");
        }

        term.DeletedAt = timeProvider.GetUtcNow();

        auditScope.Tag(term, AuditAction.DELETE, $"Xoá học kỳ {term.Code}.");

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
