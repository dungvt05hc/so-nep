using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Authorization;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;
using NeNep.Infrastructure.Security;

namespace NeNep.Api.Features.Accounts;

/// <summary>
/// Issues a login to a class officer. The homeroom teacher does this for the students of
/// their own class; nobody creates their own account.
/// <para>
/// The account starts with a temporary password and <c>must_change_password</c> set, so
/// the password written on the slip of paper stops working the moment the student signs
/// in for the first time.
/// </para>
/// </summary>
public static class CreateOfficerAccount
{
    public sealed record Request(int StudentId, Role Role, string? Username);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Role)
                .Must(RoleGroups.IsClassOfficer)
                .WithMessage("Chỉ cấp tài khoản cho lớp trưởng, phó học tập hoặc phó lao động.");

            RuleFor(x => x.Username)
                .MinimumLength(4).WithMessage("Tên đăng nhập phải có ít nhất 4 ký tự.")
                .MaximumLength(50)
                .Matches("^[A-Za-z0-9._-]+$")
                .WithMessage("Tên đăng nhập chỉ gồm chữ, số và các ký tự . _ -")
                .When(x => !string.IsNullOrWhiteSpace(x.Username));
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/accounts", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("CreateOfficerAccount")
            .WithSummary("Cấp tài khoản cán bộ lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ICurrentUser currentUser,
        IPasswordService passwords,
        IAuditScope auditScope,
        IOptions<AuthOptions> options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken)
            ?? throw new AppValidationException(nameof(Request.StudentId), "Không tìm thấy học sinh.");

        var enrolled = await db.Enrollments.AnyAsync(
            e => e.ClassId == classId && e.StudentId == student.Id && e.IsActive,
            cancellationToken);

        if (!enrolled)
        {
            throw new AppValidationException(
                nameof(Request.StudentId),
                "Học sinh không có trong danh sách lớp.");
        }

        if (await db.Users.AnyAsync(u => u.StudentId == student.Id, cancellationToken))
        {
            throw new ConflictException("Học sinh này đã có tài khoản.", "STUDENT_HAS_ACCOUNT");
        }

        var username = await ResolveUsernameAsync(db, request.Username, student, cancellationToken);
        var temporaryPassword = TemporaryPassword.Generate(options.Value.TemporaryPasswordLength);

        var user = new User
        {
            Username = username,
            PasswordHash = passwords.Hash(temporaryPassword),
            FullName = student.FullName,
            IsActive = true,
            MustChangePassword = true,
            StudentId = student.Id,
            CreatedById = currentUser.UserId,
            CreatedAt = timeProvider.GetUtcNow(),
            Roles = { new UserRole { Role = request.Role, ClassId = classId } },
        };

        db.Users.Add(user);

        auditScope.Tag(user, AuditAction.GRANT_ACCOUNT, $"Cấp tài khoản {username} cho {student.FullName}.");

        await db.SaveChangesAsync(cancellationToken);

        var account = new AccountResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.IsActive,
            user.MustChangePassword,
            user.LockedUntil,
            user.LastLoginAt,
            user.StudentId,
            student.FullName,
            request.Role,
            classId);

        return Results.Created(
            $"/api/classes/{classId}/accounts/{user.Id}",
            new IssuedAccountResponse(account, temporaryPassword));
    }

    /// <summary>
    /// Uses the username the teacher chose, or derives one from the student code and adds
    /// a number until it is free.
    /// </summary>
    private static async Task<string> ResolveUsernameAsync(
        NeNepDbContext db,
        string? requested,
        Student student,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var chosen = Usernames.Normalize(requested);

            if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == chosen, cancellationToken))
            {
                throw new ConflictException($"Tên đăng nhập \"{chosen}\" đã được sử dụng.", "USERNAME_TAKEN");
            }

            return chosen;
        }

        var baseName = Usernames.Normalize(student.Code);
        var candidate = baseName;
        var suffix = 1;

        while (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            candidate = $"{baseName}{++suffix}";
        }

        return candidate;
    }
}
