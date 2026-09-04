using NeNep.Domain.Enums;

namespace NeNep.Api.Features.AcademicYears;

public sealed record AcademicYearResponse(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent,
    IReadOnlyCollection<TermResponse> Terms);

public sealed record TermResponse(
    int Id,
    int YearId,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int Ordinal);

public sealed record WeekResponse(
    int Id,
    int YearId,
    int TermId,
    string TermCode,
    int WeekNo,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Label,
    bool IsCounted,
    string? NotCountedReason,
    DateOnly? OriginalStartDate,
    WeekStatus Status,
    DateTimeOffset? LockedAt);
