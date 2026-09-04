using FluentAssertions;
using NeNep.Domain.Calendar;

namespace NeNep.Api.Tests;

/// <summary>
/// Pure calendar arithmetic, tested without a database. These are the rules the whole
/// weekly cycle rests on.
/// </summary>
public class AcademicCalendarTests
{
    [Theory]
    [InlineData("2026-09-07", "2026-09-07")] // a Monday stays put
    [InlineData("2026-09-10", "2026-09-07")] // Thursday falls back to its Monday
    [InlineData("2026-09-13", "2026-09-07")] // Sunday belongs to the week that began on Monday
    public void MondayOf_finds_the_start_of_the_week(string date, string expected) =>
        AcademicCalendar.MondayOf(DateOnly.Parse(date))
            .Should().Be(DateOnly.Parse(expected));

    [Fact]
    public void PlanWeeks_lays_out_35_consecutive_monday_to_sunday_weeks()
    {
        // The school year opens on Saturday 5 September 2026.
        var weeks = AcademicCalendar.PlanWeeks(new DateOnly(2026, 9, 5), 35);

        weeks.Should().HaveCount(35);

        weeks[0].WeekNo.Should().Be(1);
        weeks[0].StartDate.Should().Be(new DateOnly(2026, 8, 31));
        weeks[0].EndDate.Should().Be(new DateOnly(2026, 9, 6));

        weeks.Should().OnlyContain(w => w.StartDate.DayOfWeek == DayOfWeek.Monday);
        weeks.Should().OnlyContain(w => w.EndDate.DayOfWeek == DayOfWeek.Sunday);

        weeks[34].WeekNo.Should().Be(35);
        weeks[34].StartDate.Should().Be(new DateOnly(2026, 8, 31).AddDays(34 * 7));
    }

    [Fact]
    public void PlanWeeks_can_continue_from_a_later_week_number()
    {
        var weeks = AcademicCalendar.PlanWeeks(new DateOnly(2027, 1, 4), 18, firstWeekNo: 19);

        weeks[0].WeekNo.Should().Be(19);
        weeks[^1].WeekNo.Should().Be(36);
    }

    [Theory]
    // Two whole Monday-to-Sunday blocks: a Tết break.
    [InlineData("2027-02-08", "2027-02-21", 2)]
    // Three days lost to a storm: the week is shorter, but no week disappears.
    [InlineData("2027-03-03", "2027-03-05", 0)]
    // A break that covers exactly one full week plus a few days on either side.
    [InlineData("2027-02-05", "2027-02-16", 1)]
    public void SuggestShiftWeeks_counts_the_whole_weeks_inside_the_break(
        string start,
        string end,
        int expected) =>
        AcademicCalendar.SuggestShiftWeeks(DateOnly.Parse(start), DateOnly.Parse(end))
            .Should().Be(expected);

    [Fact]
    public void PlanWeeks_refuses_a_week_count_below_one() =>
        FluentActions.Invoking(() => AcademicCalendar.PlanWeeks(new DateOnly(2026, 9, 5), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
}
