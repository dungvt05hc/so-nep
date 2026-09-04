using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Authorization;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.ClassOfficers;

public sealed record ClassOfficerResponse(
    int Id,
    int ClassId,
    int StudentId,
    string StudentName,
    Role Role,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsCurrent);

/// <summary>
/// Appoints a class monitor or one of the two deputies, for a period.
/// <para>
/// The appointment is dated because officers change during the year, and a record entered
/// in October has to stay attributable to whoever held the post in October.
/// </para>
/// </summary>
public static class AssignClassOfficer
{
    public sealed record Request(int StudentId, Role Role, DateOnly ValidFrom, DateOnly? ValidTo);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Role)
                .Must(RoleGroups.IsClassOfficer)
                .WithMessage("Chức vụ phải là lớp trưởng, phó học tập hoặc phó lao động.");

            RuleFor(x => x.ValidTo)
                .GreaterThanOrEqualTo(x => x.ValidFrom)
                .When(x => x.ValidTo is not null)
                .WithMessage("Ngày kết thúc không được trước ngày bắt đầu.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/officers", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("AssignClassOfficer")
            .WithSummary("Gán chức vụ cán bộ lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var enrolled = await db.Enrollments.AnyAsync(
            e => e.ClassId == classId && e.StudentId == request.StudentId && e.IsActive,
            cancellationToken);

        if (!enrolled)
        {
            throw new AppValidationException(
                nameof(Request.StudentId),
                "Học sinh không có trong danh sách lớp.");
        }

        // Two people holding the same post at the same time would make the anti-favouritism
        // checks in Phase 2 ambiguous, so overlapping appointments are refused.
        var overlapping = await db.ClassOfficers.AnyAsync(
            o => o.ClassId == classId
                && o.Role == request.Role
                && (o.ValidTo == null || o.ValidTo >= request.ValidFrom)
                && (request.ValidTo == null || o.ValidFrom <= request.ValidTo),
            cancellationToken);

        if (overlapping)
        {
            throw new ConflictException(
                "Lớp đã có học sinh giữ chức vụ này trong khoảng thời gian đó. Hãy kết thúc chức vụ cũ trước.",
                "OFFICER_ROLE_TAKEN");
        }

        var officer = new ClassOfficer
        {
            ClassId = classId,
            StudentId = request.StudentId,
            Role = request.Role,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
        };

        db.ClassOfficers.Add(officer);

        await db.SaveChangesAsync(cancellationToken);

        var today = VietnamClock.Today(timeProvider);

        var studentName = await db.Students
            .Where(s => s.Id == officer.StudentId)
            .Select(s => s.FullName)
            .FirstAsync(cancellationToken);

        return Results.Created(
            $"/api/classes/{classId}/officers/{officer.Id}",
            new ClassOfficerResponse(
                officer.Id,
                officer.ClassId,
                officer.StudentId,
                studentName,
                officer.Role,
                officer.ValidFrom,
                officer.ValidTo,
                officer.ValidFrom <= today && (officer.ValidTo is null || officer.ValidTo >= today)));
    }
}
