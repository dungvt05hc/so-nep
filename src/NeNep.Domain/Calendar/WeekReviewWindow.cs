using NeNep.Domain.Time;

namespace NeNep.Domain.Calendar;

/// <summary>
/// When a week stops accepting reviews.
/// <para>
/// The school locks the week on a fixed weekday and hour and then allows a grace period;
/// after that, records still waiting are discarded (or auto-approved) by the weekly lock.
/// All three numbers are configuration read from <c>school_settings</c> and are passed in
/// — never written down here (PRINCIPLE 1).
/// </para>
/// <para>
/// The wall-clock times are Vietnamese: "Sunday 20:00" means 20:00 in Ho Chi Minh City,
/// whatever the server thinks the time is.
/// </para>
/// </summary>
public static class WeekReviewWindow
{
    /// <summary>
    /// The day the week is locked on: the last day inside the week that falls on
    /// <paramref name="lockDayOfWeek"/> (0 = Sunday, matching <see cref="DayOfWeek"/>).
    /// A week that does not contain that weekday is locked on its last day.
    /// </summary>
    public static DateOnly LockDate(DateOnly weekStart, DateOnly weekEnd, int lockDayOfWeek)
    {
        for (var date = weekEnd; date >= weekStart; date = date.AddDays(-1))
        {
            if ((int)date.DayOfWeek == lockDayOfWeek)
            {
                return date;
            }
        }

        return weekEnd;
    }

    /// <summary>The instant the week closes, as a UTC-anchored moment.</summary>
    public static DateTimeOffset LockMoment(
        DateOnly weekStart,
        DateOnly weekEnd,
        int lockDayOfWeek,
        int lockHour) =>
        VietnamClock.AtLocalTime(
            LockDate(weekStart, weekEnd, lockDayOfWeek),
            new TimeOnly(lockHour, 0));

    /// <summary>
    /// The last moment a pending record can still be reviewed: the lock moment plus the
    /// grace period.
    /// </summary>
    public static DateTimeOffset ReviewDeadline(
        DateOnly weekStart,
        DateOnly weekEnd,
        int lockDayOfWeek,
        int lockHour,
        int graceHours) =>
        LockMoment(weekStart, weekEnd, lockDayOfWeek, lockHour).AddHours(graceHours);
}
