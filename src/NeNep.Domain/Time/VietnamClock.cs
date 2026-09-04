namespace NeNep.Domain.Time;

/// <summary>
/// The single place that knows the school runs on Vietnam time.
/// <para>
/// Instants are stored as UTC (<c>timestamptz</c>), but every business question about
/// dates — "what day is it", "which week does this belong to", "is the remediation
/// deadline over" — must be answered in Vietnam local time, or a record entered at
/// 21:00 on a Sunday lands in the wrong week.
/// </para>
/// <para>
/// The current instant always comes from an injected <see cref="TimeProvider"/>,
/// never from <c>DateTime.Now</c>, so tests can move time forward.
/// </para>
/// </summary>
public static class VietnamClock
{
    /// <summary>IANA identifier; .NET resolves it on both Linux and Windows.</summary>
    public const string TimeZoneId = "Asia/Ho_Chi_Minh";

    public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>The instant expressed in Vietnam local time.</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, TimeZone);

    /// <summary>The business date an instant falls on, in Vietnam local time.</summary>
    public static DateOnly ToLocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(ToLocal(instant).DateTime);

    /// <summary>Today's business date.</summary>
    public static DateOnly Today(TimeProvider timeProvider) =>
        ToLocalDate(timeProvider.GetUtcNow());

    /// <summary>
    /// The instant a Vietnam-local wall clock time corresponds to, for storing a
    /// deadline or a lock moment as <c>timestamptz</c>.
    /// </summary>
    public static DateTimeOffset AtLocalTime(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        var offset = TimeZone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }
}
