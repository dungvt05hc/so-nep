using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Authorization;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The rules that decide whether a record may be written at all, in one place so the
/// create and the edit endpoint cannot disagree about them.
/// <para>
/// Every one of these guards exists because of a specific way the paper book was abused
/// or went wrong; none of them may be skipped "because the caller is trusted".
/// </para>
/// </summary>
public static class ViolationRules
{
    /// <summary>The two statuses that occupy a slot under <c>MaxPerWeek</c>.</summary>
    private static readonly ViolationStatus[] CountedStatuses =
        [ViolationStatus.PENDING, ViolationStatus.APPROVED];

    /// <summary>
    /// The academic week a date falls in, for the class's own academic year.
    /// <para>
    /// A date outside every week — a public holiday, a Tết break, a mistyped year — has
    /// no week to belong to and the record is refused rather than parked somewhere
    /// arbitrary, which would silently distort the weekly score.
    /// </para>
    /// </summary>
    public static async Task<AcademicWeek> ResolveWeekAsync(
        NeNepDbContext db,
        int yearId,
        DateOnly occurredDate,
        CancellationToken cancellationToken) =>
        await db.AcademicWeeks
            .FirstOrDefaultAsync(
                w => w.YearId == yearId && w.StartDate <= occurredDate && w.EndDate >= occurredDate,
                cancellationToken)
        ?? throw new ConflictException(
            $"Ngày {occurredDate:dd/MM/yyyy} không thuộc tuần học nào của năm học này.",
            "NO_ACADEMIC_WEEK");

    /// <summary>
    /// A locked week has already been scored and reported to parents. PRINCIPLE 2: what
    /// was published stays as published, so nothing in that week may be written any more.
    /// </summary>
    public static void EnsureWeekIsOpen(AcademicWeek week)
    {
        if (week.Status is WeekStatus.LOCKED or WeekStatus.PUBLISHED)
        {
            throw new ConflictException(
                $"Tuần {week.WeekNo} đã chốt, không thể thêm hoặc sửa bản ghi.",
                "WEEK_LOCKED");
        }
    }

    /// <summary>
    /// Checks the caller against the code's <c>allowed_roles</c>, using the roles they
    /// hold IN THIS CLASS — being the homeroom teacher of another class means nothing here.
    /// </summary>
    public static void EnsureMayRecord(ClassScope scope, int classId, ViolationType type)
    {
        if (type.IsAutoComputed)
        {
            throw new ConflictException(
                $"Mã {type.Code} do hệ thống tự tính, không nhập tay.",
                "VIOLATION_TYPE_AUTO_COMPUTED");
        }

        var roles = scope.RolesIn(classId);

        // The school administrator acts on behalf of the school and is not part of the
        // catalog's role list, which only describes who inside a class may record what.
        if (roles.Contains(Role.ADMIN))
        {
            return;
        }

        if (!type.AllowedRoles.Any(roles.Contains))
        {
            throw new ForbiddenException(
                $"Bạn không được phép ghi mã {type.Code}. Mã này do giáo viên chủ nhiệm ghi.",
                "VIOLATION_TYPE_NOT_ALLOWED_FOR_ROLE");
        }
    }

    /// <summary>
    /// ANTI-ABUSE RULE (plan section 4): a class officer may not award themselves bonus
    /// points. Recording their own violation is fine — it is the reward side that needs
    /// the guard.
    /// </summary>
    public static void EnsureNotSelfAwardedBonus(
        ClassScope scope,
        int classId,
        ViolationType type,
        int effectivePoints,
        int? callerStudentId,
        int targetStudentId)
    {
        if (callerStudentId is null || callerStudentId != targetStudentId)
        {
            return;
        }

        if (!IsOfficerOnly(scope, classId))
        {
            return;
        }

        if (type.Category.Kind != CategoryKind.KHEN_THUONG && effectivePoints <= 0)
        {
            return;
        }

        throw new ForbiddenException(
            "Cán bộ lớp không được tự ghi điểm cộng cho chính mình.",
            "SELF_AWARDED_BONUS");
    }

    /// <summary>
    /// ANTI-ABUSE RULE (plan section 4): one class officer recording against another is
    /// flagged so the homeroom teacher looks closely when reviewing. It is not blocked —
    /// officers do break rules too — it is made visible.
    /// </summary>
    public static async Task<bool> IsPeerReportAsync(
        NeNepDbContext db,
        ClassScope scope,
        int classId,
        int targetStudentId,
        DateOnly occurredDate,
        int? callerStudentId,
        CancellationToken cancellationToken)
    {
        if (!IsOfficerOnly(scope, classId) || callerStudentId == targetStudentId)
        {
            return false;
        }

        return await db.ClassOfficers.AnyAsync(
            o => o.ClassId == classId
                && o.StudentId == targetStudentId
                && o.ValidFrom <= occurredDate
                && (o.ValidTo == null || o.ValidTo >= occurredDate),
            cancellationToken);
    }

    /// <summary>
    /// Enforces <c>max_per_week</c> — C12 and B01/B02 are once a week per student, and
    /// the auto-computed bonuses exist only once.
    /// <para>
    /// Only PENDING and APPROVED records occupy a slot: a rejected or expired record
    /// never counted towards anything, so it must not block a correct one either.
    /// </para>
    /// </summary>
    public static async Task EnsureWithinWeeklyLimitAsync(
        NeNepDbContext db,
        ViolationType type,
        int studentId,
        int weekId,
        int quantity,
        int? excludeRecordId,
        CancellationToken cancellationToken)
    {
        if (type.MaxPerWeek is not { } limit)
        {
            return;
        }

        var used = await db.ViolationRecords
            .Where(r => r.StudentId == studentId
                && r.TypeId == type.Id
                && r.WeekId == weekId
                && r.Id != excludeRecordId
                && CountedStatuses.Contains(r.Status))
            .SumAsync(r => (int?)r.Quantity, cancellationToken) ?? 0;

        if (used + quantity > limit)
        {
            throw new ConflictException(
                $"Mã {type.Code} chỉ được ghi tối đa {limit} lần/tuần cho mỗi học sinh (đã có {used}).",
                "MAX_PER_WEEK_EXCEEDED");
        }
    }

    /// <summary>
    /// Copies the remediation requirement of the code onto the record and works out the
    /// deadline from the date of the violation. Both are business DATES, so no time zone
    /// is involved (see CLAUDE.md section 4).
    /// </summary>
    public static void ApplyRemediation(ViolationRecord record, ViolationType type)
    {
        if (!type.RequiresRemediation)
        {
            record.RemediationStatus = RemediationStatus.NOT_REQUIRED;
            record.RemediationNote = null;
            record.RemediationDeadline = null;

            return;
        }

        record.RemediationStatus = RemediationStatus.PENDING;
        record.RemediationNote = type.RemediationNote;
        record.RemediationDeadline = record.OccurredDate.AddDays(type.RemediationDays ?? 0);
    }

    /// <summary>Takes the snapshot that makes the record independent of the catalog.</summary>
    public static void ApplySnapshot(ViolationRecord record, ViolationType type, int effectivePoints)
    {
        record.TypeId = type.Id;
        record.TypeCodeSnapshot = type.Code;
        record.TypeNameSnapshot = type.Name;
        record.PointsSnapshot = effectivePoints;
    }

    /// <summary>The student must be on the register of THIS class right now.</summary>
    public static async Task EnsureEnrolledAsync(
        NeNepDbContext db,
        int classId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var enrolled = await db.Enrollments.AnyAsync(
            e => e.ClassId == classId && e.StudentId == studentId && e.IsActive,
            cancellationToken);

        if (!enrolled)
        {
            throw new NotFoundException("Học sinh không thuộc danh sách lớp này.");
        }
    }

    /// <summary>The student this account belongs to, or null for a teacher account.</summary>
    public static async Task<int?> GetCallerStudentIdAsync(
        NeNepDbContext db,
        int userId,
        CancellationToken cancellationToken) =>
        await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.StudentId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// True when the caller is a class officer here and nothing more. A homeroom teacher
    /// who also happens to be recorded as an officer is not subject to the officer rules.
    /// </summary>
    private static bool IsOfficerOnly(ClassScope scope, int classId)
    {
        var roles = scope.RolesIn(classId);

        return roles.Any(RoleGroups.IsClassOfficer)
            && !roles.Contains(Role.GVCN)
            && !roles.Contains(Role.ADMIN);
    }

    /// <summary>
    /// A record entered by the homeroom teacher (or the administrator on their behalf) is
    /// already reviewed by definition — they ARE the review step. Everything a class
    /// officer enters waits for them.
    /// </summary>
    public static bool ReviewsOwnRecords(ClassScope scope, int classId) => !IsOfficerOnly(scope, classId);
}
