using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Entities;

namespace NeNep.Api.Tests;

/// <summary>
/// The three "what is missing" reports: days nobody wrote anything on, pupils nobody
/// wrote anything about, and records nobody reviewed in time.
/// <para>
/// They are the counterweight to the rest of the system. Everything else answers "what
/// was recorded"; these answer "what should have been and was not", which is how a class
/// that quietly stopped using the book gets noticed before the term ends.
/// </para>
/// </summary>
[Collection(ApiCollection.Name)]
public class MissingDataReportTests
{
    private readonly ApiFactory _factory;

    public MissingDataReportTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_days_of_a_week_with_no_record_are_listed()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        // Two records on the Tuesday of the week that is over, none on any other day.
        foreach (var code in new[] { "N01", "H01" })
        {
            (await teacher.PostAsJsonAsync(
                    $"/api/classes/{fixture.ClassId}/violations",
                    new
                    {
                        studentId = fixture.StudentId,
                        typeId = await fixture.TypeIdAsync(code),
                        occurredDate = "2026-09-01",
                    }))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var body = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/reports/days-without-records?weekId={fixture.PreviousWeekId}"))
            .ReadJsonAsync();

        body.GetProperty("weekNo").GetInt32().Should().Be(1);
        body.GetProperty("startDate").GetString().Should().Be("2026-08-31");

        var days = body.GetProperty("days").EnumerateArray()
            .Select(d => d.GetProperty("date").GetString())
            .ToList();

        days.Should().NotContain("2026-09-01");
        days.Should().Equal(
            "2026-08-31",
            "2026-09-02",
            "2026-09-03",
            "2026-09-04",
            "2026-09-05",
            "2026-09-06");

        body.GetProperty("days").EnumerateArray()
            .Single(d => d.GetProperty("date").GetString() == "2026-09-06")
            .GetProperty("isWeekend").GetBoolean()
            .Should().BeTrue();
    }

    [Fact]
    public async Task A_confirmed_school_break_is_not_counted_as_missing_data()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        await using (var db = _factory.CreateDbContext())
        {
            db.SchoolBreaks.Add(new SchoolBreak
            {
                YearId = fixture.School.YearId,
                Name = "Nghỉ Quốc khánh",
                StartDate = new DateOnly(2026, 9, 2),
                EndDate = new DateOnly(2026, 9, 3),
                IsConfirmed = true,
            });

            await db.SaveChangesAsync();
        }

        var body = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/reports/days-without-records?weekId={fixture.PreviousWeekId}"))
            .ReadJsonAsync();

        var days = body.GetProperty("days").EnumerateArray()
            .Select(d => d.GetProperty("date").GetString())
            .ToList();

        days.Should().NotContain("2026-09-02");
        days.Should().NotContain("2026-09-03");

        body.GetProperty("breakDates").EnumerateArray()
            .Select(d => d.GetString())
            .Should().Equal("2026-09-02", "2026-09-03");
    }

    [Fact]
    public async Task The_pupils_nobody_has_written_anything_about_are_listed_by_week_and_by_term()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("C01"),
                occurredDate = "2026-09-07",
            });

        var thisWeek = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/reports/students-without-records?weekId={fixture.CurrentWeekId}"))
            .ReadJsonAsync();

        var ids = thisWeek.EnumerateArray().Select(s => s.GetProperty("studentId").GetInt32()).ToList();

        ids.Should().NotContain(fixture.StudentId);
        ids.Should().BeEquivalentTo([fixture.OtherStudentId, fixture.OfficerStudentId]);

        // The pupils are ordered the way the class register is.
        thisWeek.EnumerateArray().Select(s => s.GetProperty("orderNo").GetInt32())
            .Should().BeInAscendingOrder();

        // Over the whole term the same pupil is covered, because the record is in it.
        var term = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/reports/students-without-records?termId={fixture.School.Hk1Id}"))
            .ReadJsonAsync();

        term.EnumerateArray().Select(s => s.GetProperty("studentId").GetInt32())
            .Should().NotContain(fixture.StudentId);

        // A different week has nobody covered at all.
        var otherWeek = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/reports/students-without-records?weekId={fixture.PreviousWeekId}"))
            .ReadJsonAsync();

        otherWeek.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Asking_for_both_a_week_and_a_term_or_for_neither_is_refused()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        (await teacher.GetAsync($"/api/classes/{fixture.ClassId}/reports/students-without-records"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await teacher.GetAsync(
                $"/api/classes/{fixture.ClassId}/reports/students-without-records"
                + $"?weekId={fixture.CurrentWeekId}&termId={fixture.School.Hk1Id}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_pending_record_shows_up_once_its_review_window_has_closed()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();
        var officer = await fixture.SignInOfficerAsync();

        // Entered on the Friday of the week that is over, and never reviewed. That week
        // locked on Sunday 6 September at 20:00 Vietnam time, plus 16 hours of grace, so
        // it was overdue from noon on Monday 7 September — and the clock says 08:00.
        var stillInTime = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-04",
            });

        stillInTime.StatusCode.Should().Be(HttpStatusCode.Created);

        var probe = await teacher.GetAsync($"/api/classes/{fixture.ClassId}/reports/overdue-pending");

        probe.StatusCode.Should().Be(HttpStatusCode.OK, await probe.Content.ReadAsStringAsync());

        var beforeDeadline = await probe.ReadJsonAsync();

        beforeDeadline.GetArrayLength().Should().Be(0, "the grace period has not run out yet");

        _factory.Time.Advance(TimeSpan.FromHours(6));

        try
        {
            // The clock moved, so the earlier access token is past its expiry.
            var later = await fixture.SignInTeacherAsync();

            var body = await (await later.GetAsync(
                $"/api/classes/{fixture.ClassId}/reports/overdue-pending")).ReadJsonAsync();

            body.GetArrayLength().Should().Be(1);

            var row = body[0];

            row.GetProperty("typeCode").GetString().Should().Be("N01");
            row.GetProperty("weekNo").GetInt32().Should().Be(1);
            row.GetProperty("studentId").GetInt32().Should().Be(fixture.OtherStudentId);
            row.GetProperty("reviewDeadline").GetString().Should().StartWith("2026-09-07T12:00:00");
            row.GetProperty("hoursOverdue").GetInt32().Should().Be(2);
        }
        finally
        {
            _factory.Time.Advance(TimeSpan.FromHours(-6));
        }
    }

    [Fact]
    public async Task An_approved_record_never_appears_as_overdue()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        // Entered by the homeroom teacher, so it is approved from the start.
        await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-04",
            });

        _factory.Time.Advance(TimeSpan.FromDays(3));

        try
        {
            var later = await fixture.SignInTeacherAsync();

            var body = await (await later.GetAsync(
                $"/api/classes/{fixture.ClassId}/reports/overdue-pending")).ReadJsonAsync();

            body.GetArrayLength().Should().Be(0);
        }
        finally
        {
            _factory.Time.Advance(TimeSpan.FromDays(-3));
        }
    }

    [Fact]
    public async Task The_reports_stop_at_the_class_boundary()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher91 = await fixture.SignInTeacherAsync();

        var other = fixture.School.Class92Id;

        (await teacher91.GetAsync(
                $"/api/classes/{other}/reports/days-without-records?weekId={fixture.CurrentWeekId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await teacher91.GetAsync(
                $"/api/classes/{other}/reports/students-without-records?weekId={fixture.CurrentWeekId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await teacher91.GetAsync($"/api/classes/{other}/reports/overdue-pending"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
