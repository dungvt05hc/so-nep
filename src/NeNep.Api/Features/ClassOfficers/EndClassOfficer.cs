using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.ClassOfficers;

/// <summary>
/// Ends an officer appointment by dating it, never by deleting it: records entered while
/// the student held the post must stay explainable afterwards.
/// <para>
/// The account is a separate matter. Ending the appointment does not lock the account, so
/// the teacher can hand the post over first and revoke the login when they are ready.
/// </para>
/// </summary>
public static class EndClassOfficer
{
    public sealed record Request(DateOnly? ValidTo);

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/officers/{officerId:int}/end", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("EndClassOfficer")
            .WithSummary("Kết thúc chức vụ cán bộ lớp");

    private static async Task<ClassOfficerResponse> HandleAsync(
        int classId,
        int officerId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var officer = await db.ClassOfficers
            .Include(o => o.Student)
            .FirstOrDefaultAsync(o => o.Id == officerId && o.ClassId == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chức vụ cán bộ lớp.");

        var today = VietnamClock.Today(timeProvider);
        var validTo = request.ValidTo ?? today;

        if (validTo < officer.ValidFrom)
        {
            throw new AppValidationException(
                nameof(Request.ValidTo),
                "Ngày kết thúc không được trước ngày bắt đầu.");
        }

        officer.ValidTo = validTo;

        auditScope.Tag(officer, AuditAction.UPDATE, $"Kết thúc chức vụ {officer.Role} từ {validTo:dd/MM/yyyy}.");

        await db.SaveChangesAsync(cancellationToken);

        return new ClassOfficerResponse(
            officer.Id,
            officer.ClassId,
            officer.StudentId,
            officer.Student.FullName,
            officer.Role,
            officer.ValidFrom,
            officer.ValidTo,
            officer.ValidFrom <= today && officer.ValidTo >= today);
    }
}
