using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>
/// Takes a student off the class register when they move away or change class.
/// <para>
/// Nothing is deleted: the enrollment is closed with a date and a note, so every record
/// the student left behind keeps pointing at a row that still exists, and last term's
/// report still adds up.
/// </para>
/// </summary>
public static class EndEnrollment
{
    public sealed record Request(DateOnly? LeftAt, string? LeaveNote);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.LeaveNote).MaximumLength(255);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/students/{studentId:int}/leave", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("EndEnrollment")
            .WithSummary("Cho học sinh rời lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        int studentId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("Học sinh không thuộc lớp này.");

        if (!enrollment.IsActive)
        {
            throw new ConflictException("Học sinh đã rời lớp trước đó.");
        }

        enrollment.IsActive = false;
        enrollment.LeftAt = request.LeftAt ?? VietnamClock.Today(timeProvider);
        enrollment.LeaveNote = string.IsNullOrWhiteSpace(request.LeaveNote) ? null : request.LeaveNote.Trim();

        auditScope.Tag(enrollment, AuditAction.UPDATE, "Học sinh rời lớp.");

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
