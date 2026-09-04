using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests;

/// <summary>
/// Generating the weeks of a year, and pushing them back when the school closes for a
/// break. The week NUMBER is what the school talks in, so it must survive every shift.
/// </summary>
[Collection(ApiCollection.Name)]
public class SchoolCalendarTests
{
    private readonly ApiFactory _factory;

    public SchoolCalendarTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generating_weeks_splits_them_across_the_terms_and_is_safe_to_repeat()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var response = await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadJsonAsync();

        result.GetProperty("created").GetInt32().Should().Be(35);

        var weeks = await (await admin.GetAsync($"/api/academic-years/{school.YearId}/weeks")).ReadJsonAsync();

        weeks.GetArrayLength().Should().Be(35);

        var terms = weeks.EnumerateArray().Select(w => w.GetProperty("termCode").GetString()).ToList();

        terms.Should().Contain("HK1").And.Contain("HK2");

        // Running it again changes nothing: no duplicates, no new rows.
        var again = await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        var repeated = await again.ReadJsonAsync();

        repeated.GetProperty("created").GetInt32().Should().Be(0);
        repeated.GetProperty("moved").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Applying_a_break_moves_the_open_weeks_and_keeps_their_numbers()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        var created = await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/breaks",
            new
            {
                name = "Nghỉ Tết Đinh Mùi",
                startDate = "2027-02-08",
                endDate = "2027-02-21",
                isConfirmed = true,
            });

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var breakId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        Dictionary<int, DateOnly> before;

        await using (var db = _factory.CreateDbContext())
        {
            before = await db.AcademicWeeks
                .Where(w => w.YearId == school.YearId)
                .ToDictionaryAsync(w => w.WeekNo, w => w.StartDate);
        }

        var preview = await admin.PostAsJsonAsync(
            $"/api/breaks/{breakId}/apply",
            new { preview = true });

        var previewBody = await preview.ReadJsonAsync();

        previewBody.GetProperty("applied").GetBoolean().Should().BeFalse();
        previewBody.GetProperty("suggestedShiftWeeks").GetInt32().Should().Be(2);

        var applied = await admin.PostAsJsonAsync($"/api/breaks/{breakId}/apply", new { preview = false });

        applied.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await applied.ReadJsonAsync();

        body.GetProperty("shiftWeeks").GetInt32().Should().Be(2);

        await using (var db = _factory.CreateDbContext())
        {
            var after = await db.AcademicWeeks
                .Where(w => w.YearId == school.YearId)
                .ToListAsync();

            after.Should().HaveCount(35, "shifting must never add or remove a week");

            foreach (var week in after)
            {
                var wasStart = before[week.WeekNo];

                if (wasStart >= new DateOnly(2027, 2, 8))
                {
                    week.StartDate.Should().Be(wasStart.AddDays(14));
                    week.OriginalStartDate.Should().Be(wasStart);
                }
                else
                {
                    week.StartDate.Should().Be(wasStart);
                    week.OriginalStartDate.Should().BeNull();
                }
            }
        }
    }

    [Fact]
    public async Task A_break_is_refused_when_a_locked_week_would_have_to_move()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        await using (var db = _factory.CreateDbContext())
        {
            var week = await db.AcademicWeeks
                .Where(w => w.YearId == school.YearId && w.StartDate >= new DateOnly(2027, 2, 8))
                .OrderBy(w => w.WeekNo)
                .FirstAsync();

            week.Status = WeekStatus.LOCKED;
            week.LockedAt = _factory.Time.GetUtcNow();

            await db.SaveChangesAsync();
        }

        var created = await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/breaks",
            new
            {
                name = "Nghỉ bão số 5",
                startDate = "2027-02-08",
                endDate = "2027-02-21",
                isConfirmed = true,
            });

        var breakId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        var applied = await admin.PostAsJsonAsync($"/api/breaks/{breakId}/apply", new { preview = false });

        applied.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await applied.ReadJsonAsync()).GetProperty("code").GetString()
            .Should().Be("BREAK_HITS_LOCKED_WEEKS");
    }

    [Fact]
    public async Task A_locked_week_cannot_be_edited()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 4 });

        int weekId;

        await using (var db = _factory.CreateDbContext())
        {
            var week = await db.AcademicWeeks.FirstAsync(w => w.YearId == school.YearId && w.WeekNo == 1);

            week.Status = WeekStatus.LOCKED;
            weekId = week.Id;

            await db.SaveChangesAsync();
        }

        var response = await admin.PutAsJsonAsync(
            $"/api/weeks/{weekId}",
            new { label = "Tuần kiểm tra HK1", isCounted = false, notCountedReason = "Tuần kiểm tra" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("WEEK_LOCKED");
    }
}
