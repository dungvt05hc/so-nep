namespace NeNep.Api.Features.Reports;

/// <summary>
/// A day inside the week on which the class recorded nothing at all.
/// <para>
/// Not necessarily a fault — a quiet Tuesday is a good Tuesday — but a whole week of
/// them usually means the class stopped entering data, which is exactly what the school
/// board's "tình trạng nhập liệu" board is for.
/// </para>
/// </summary>
/// <param name="IsWeekend">Saturday or Sunday, so the client can dim the row.</param>
public sealed record DayWithoutRecordsResponse(
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsWeekend);

/// <summary>Days of one week with no record, plus the days that were a school break.</summary>
/// <param name="BreakDates">Dates covered by a confirmed break, excluded from the report.</param>
public sealed record MissingDaysResponse(
    int WeekId,
    int WeekNo,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<DayWithoutRecordsResponse> Days,
    IReadOnlyList<DateOnly> BreakDates);

/// <summary>
/// A student with no record at all in the period — neither a violation nor a
/// commendation.
/// <para>
/// The anti-favouritism report of the plan (section 10): a pupil nobody ever writes
/// anything about is as much a data problem as one who is written up every day.
/// </para>
/// </summary>
public sealed record StudentWithoutRecordsResponse(
    int StudentId,
    string Code,
    string FullName,
    int? OrderNo);

/// <summary>
/// A record still waiting for review after the week's review window has closed.
/// <para>
/// The deadline is the week's lock moment plus the grace period, both read from
/// <c>school_settings</c>. Once it passes, the weekly lock discards the record, so the
/// homeroom teacher has to see it before that.
/// </para>
/// </summary>
public sealed record OverduePendingResponse(
    int Id,
    int WeekId,
    int WeekNo,
    int StudentId,
    string StudentCode,
    string StudentName,
    string TypeCode,
    string TypeName,
    DateOnly OccurredDate,
    int ReportedById,
    string ReportedByName,
    DateTimeOffset ReportedAt,
    DateTimeOffset ReviewDeadline,
    int HoursOverdue);
