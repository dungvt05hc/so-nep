using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// One record as the review queue and the class register show it.
/// <para>
/// The code, the name and the points come from the SNAPSHOT taken when the record was
/// created, never from the catalog as it stands today. Editing the catalog must not
/// change a report already sent to parents.
/// </para>
/// </summary>
public sealed record ViolationResponse(
    int Id,
    int ClassId,
    int StudentId,
    string StudentCode,
    string StudentName,
    int WeekId,
    int WeekNo,
    WeekStatus WeekStatus,
    int TypeId,
    string TypeCode,
    string TypeName,
    int Points,
    int Quantity,
    int TotalPoints,
    bool CountsForScore,
    DateOnly OccurredDate,
    int? PeriodNo,
    string? Note,
    ViolationStatus Status,
    string? RejectReason,
    bool IsFlagged,
    RemediationStatus RemediationStatus,
    string? RemediationNote,
    DateOnly? RemediationDeadline,
    int ReportedById,
    string ReportedByName,
    DateTimeOffset ReportedAt,
    int? ReviewedById,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    bool IsLocked);

/// <summary>A page of the review queue.</summary>
public sealed record ViolationPageResponse(
    IReadOnlyList<ViolationResponse> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>The one projection every violation endpoint returns its rows through.</summary>
public static class ViolationProjection
{
    public static readonly Expression<Func<ViolationRecord, ViolationResponse>> ToResponse = r =>
        new ViolationResponse(
            r.Id,
            r.ClassId,
            r.StudentId,
            r.Student.Code,
            r.Student.FullName,
            r.WeekId,
            r.Week.WeekNo,
            r.Week.Status,
            r.TypeId,
            r.TypeCodeSnapshot,
            r.TypeNameSnapshot,
            r.PointsSnapshot,
            r.Quantity,
            r.PointsSnapshot * r.Quantity,
            r.Type.CountsForScore,
            r.OccurredDate,
            r.PeriodNo,
            r.Note,
            r.Status,
            r.RejectReason,
            r.IsFlagged,
            r.RemediationStatus,
            r.RemediationNote,
            r.RemediationDeadline,
            r.ReportedById,
            r.ReportedBy.FullName,
            r.ReportedAt,
            r.ReviewedById,
            r.ReviewedBy != null ? r.ReviewedBy.FullName : null,
            r.ReviewedAt,
            r.IsLocked);

    /// <summary>Reads one record back after it was written, for the endpoint's response.</summary>
    public static async Task<ViolationResponse> LoadAsync(
        NeNepDbContext db,
        int id,
        CancellationToken cancellationToken) =>
        await db.ViolationRecords
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bản ghi.");
}
