namespace NeNep.Domain.Calendar;

/// <summary>
/// One planned academic week: a Monday-to-Sunday block carrying the number the school
/// uses for it.
/// </summary>
/// <param name="WeekNo">Sequence number of the week within the academic year.</param>
public sealed record PlannedWeek(int WeekNo, DateOnly StartDate, DateOnly EndDate);

/// <summary>
/// Pure calendar arithmetic for the academic year. No EF, no DI, no clock: everything
/// is derived from the dates handed in, so it can be unit tested on its own.
/// <para>
/// An academic week always runs Monday to Sunday, because the week is locked on Sunday
/// evening. A week keeps its <see cref="PlannedWeek.WeekNo"/> for the whole year, even
/// when a school break pushes its dates back — see <c>SchoolBreak</c>.
/// </para>
/// </summary>
public static class AcademicCalendar
{
    /// <summary>Days in a week. A calendar fact, not a rule of the school regulation.</summary>
    public const int DaysPerWeek = 7;

    /// <summary>The Monday of the calendar week that contains <paramref name="date"/>.</summary>
    public static DateOnly MondayOf(DateOnly date)
    {
        // DayOfWeek starts at Sunday = 0, so Sunday has to fall back a full six days.
        var offset = date.DayOfWeek == DayOfWeek.Sunday
            ? DaysPerWeek - 1
            : (int)date.DayOfWeek - (int)DayOfWeek.Monday;

        return date.AddDays(-offset);
    }

    /// <summary>The Sunday that closes the calendar week containing <paramref name="date"/>.</summary>
    public static DateOnly SundayOf(DateOnly date) => MondayOf(date).AddDays(DaysPerWeek - 1);

    /// <summary>
    /// Lays out <paramref name="weekCount"/> consecutive academic weeks starting from the
    /// week that contains the first day of school. The school's own numbering starts at
    /// <paramref name="firstWeekNo"/>.
    /// </summary>
    /// <remarks>
    /// The number of weeks is an input, never a constant: the school announces it in its
    /// yearly plan (35 weeks for 2026-2027) and it can change.
    /// </remarks>
    public static IReadOnlyList<PlannedWeek> PlanWeeks(
        DateOnly firstSchoolDay,
        int weekCount,
        int firstWeekNo = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(weekCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstWeekNo, 1);

        var monday = MondayOf(firstSchoolDay);
        var weeks = new List<PlannedWeek>(weekCount);

        for (var i = 0; i < weekCount; i++)
        {
            var start = monday.AddDays(i * DaysPerWeek);

            weeks.Add(new PlannedWeek(firstWeekNo + i, start, start.AddDays(DaysPerWeek - 1)));
        }

        return weeks;
    }

    /// <summary>
    /// How many academic weeks a break swallows: the number of whole Monday-to-Sunday
    /// blocks that fit inside it.
    /// <para>
    /// A two-week Tết break therefore shifts the calendar by two weeks, while a three-day
    /// closure shifts nothing — the week still happens, it is just shorter. The result is
    /// only a SUGGESTION: the administrator confirms or overrides it when applying the
    /// break, because only the school knows whether a short closure cost them a full week.
    /// </para>
    /// </summary>
    public static int SuggestShiftWeeks(DateOnly breakStart, DateOnly breakEnd)
    {
        if (breakEnd < breakStart)
        {
            throw new ArgumentException("Break end date is before its start date.", nameof(breakEnd));
        }

        // The first Monday on or after the break starts.
        var monday = breakStart.DayOfWeek == DayOfWeek.Monday ? breakStart : MondayOf(breakStart).AddDays(DaysPerWeek);
        var whole = 0;

        while (monday.AddDays(DaysPerWeek - 1) <= breakEnd)
        {
            whole++;
            monday = monday.AddDays(DaysPerWeek);
        }

        return whole;
    }

    /// <summary>True when two date ranges share at least one day.</summary>
    public static bool Overlaps(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd) =>
        aStart <= bEnd && bStart <= aEnd;
}
