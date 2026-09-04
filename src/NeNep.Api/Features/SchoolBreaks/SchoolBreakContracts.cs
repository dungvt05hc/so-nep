namespace NeNep.Api.Features.SchoolBreaks;

public sealed record SchoolBreakResponse(
    int Id,
    int YearId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsConfirmed,
    int ShiftedWeeks,
    DateTimeOffset? AppliedAt,
    int? AppliedById,
    string? Note);

/// <summary>One week moved, or about to be moved, by a school break.</summary>
public sealed record ShiftedWeekResponse(
    int WeekId,
    int WeekNo,
    DateOnly FromStartDate,
    DateOnly ToStartDate,
    DateOnly ToEndDate);

public sealed record ApplySchoolBreakResponse(
    int BreakId,
    bool Applied,
    int ShiftWeeks,
    int SuggestedShiftWeeks,
    IReadOnlyCollection<ShiftedWeekResponse> Weeks);
