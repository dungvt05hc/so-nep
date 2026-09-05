using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests;

/// <summary>
/// THE ACCEPTANCE TEST OF PHASE 2, review side: the life of a record from the class
/// monitor's phone to APPROVED, REJECTED or gone — and the audit row that every one of
/// those steps leaves behind with its before and after.
/// </summary>
[Collection(ApiCollection.Name)]
public class ViolationReviewTests
{
    private readonly ApiFactory _factory;

    public ViolationReviewTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Approving_a_record_records_the_reviewer_and_an_APPROVE_audit_row()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        teacher.DefaultRequestHeaders.Add("User-Agent", "NeNep-Test/1.0");

        var response = await teacher.PostAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/approve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();

        body.GetProperty("status").GetString().Should().Be(nameof(ViolationStatus.APPROVED));
        body.GetProperty("reviewedById").GetInt32().Should().Be(fixture.School.Teacher91Id);
        body.GetProperty("reviewedAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        var log = await LastLogAsync(id);

        log.Action.Should().Be(AuditAction.APPROVE);
        log.ActorId.Should().Be(fixture.School.Teacher91Id);
        log.ActorRole.Should().Be(Role.GVCN);
        log.ClassId.Should().Be(fixture.ClassId);
        log.Summary.Should().Be("Duyệt N01");

        // Before and after hold the columns that moved, and only those.
        log.BeforeJson!.RootElement.GetProperty("Status").GetString()
            .Should().Be(nameof(ViolationStatus.PENDING));
        log.AfterJson!.RootElement.GetProperty("Status").GetString()
            .Should().Be(nameof(ViolationStatus.APPROVED));

        // The device the change came from. The IP is filled from the connection, which the
        // in-memory test host does not have; the user agent proves the plumbing.
        log.UserAgent.Should().Be("NeNep-Test/1.0");
    }

    [Fact]
    public async Task A_record_can_only_be_reviewed_once()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        await teacher.PostAsync($"/api/classes/{fixture.ClassId}/violations/{id}/approve", content: null);

        var again = await teacher.PostAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/approve",
            content: null);

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("VIOLATION_NOT_PENDING");

        var rejected = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/reject",
            new { reason = "Đổi ý" });

        rejected.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Rejecting_needs_a_reason_and_keeps_it_for_the_class_officer_to_read()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();
        var officer = await fixture.SignInOfficerAsync();

        var id = await ReportAsync(fixture, "N01");

        var empty = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/reject",
            new { reason = "" });

        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await empty.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/reject",
            new { reason = "Em này hôm đó nghỉ có phép" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();

        body.GetProperty("status").GetString().Should().Be(nameof(ViolationStatus.REJECTED));
        body.GetProperty("rejectReason").GetString().Should().Be("Em này hôm đó nghỉ có phép");

        // The class officer sees the outcome of what they entered.
        var mine = await (await officer.GetAsync(
            $"/api/classes/{fixture.ClassId}/violations")).ReadJsonAsync();

        mine.GetProperty("items").EnumerateArray()
            .Single(r => r.GetProperty("id").GetInt32() == id)
            .GetProperty("rejectReason").GetString()
            .Should().Be("Em này hôm đó nghỉ có phép");

        (await LastLogAsync(id)).Action.Should().Be(AuditAction.REJECT);
    }

    [Fact]
    public async Task The_whole_selection_is_approved_in_one_go_and_says_what_it_skipped()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var first = await ReportAsync(fixture, "N01");
        var second = await ReportAsync(fixture, "H01");
        var third = await ReportAsync(fixture, "V02");

        // The third one is dealt with beforehand, so the bulk call has to step over it.
        await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{third}/reject",
            new { reason = "Trùng bản ghi" });

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/approve",
            new { ids = new[] { first, second, third, 999_999 } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();

        body.GetProperty("approved").EnumerateArray().Select(e => e.GetInt32())
            .Should().BeEquivalentTo([first, second]);

        var skipped = body.GetProperty("skipped").EnumerateArray().ToList();

        skipped.Should().HaveCount(2);
        skipped.Select(s => s.GetProperty("code").GetString())
            .Should().BeEquivalentTo(["VIOLATION_NOT_PENDING", "NOT_FOUND"]);

        await using var db = _factory.CreateDbContext();

        var statuses = await db.ViolationRecords
            .Where(r => r.ClassId == fixture.ClassId)
            .ToDictionaryAsync(r => r.Id, r => r.Status);

        statuses[first].Should().Be(ViolationStatus.APPROVED);
        statuses[second].Should().Be(ViolationStatus.APPROVED);
        statuses[third].Should().Be(ViolationStatus.REJECTED);
    }

    [Fact]
    public async Task Correcting_the_code_re_takes_the_snapshot()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        var response = await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}",
            new
            {
                typeId = await fixture.TypeIdAsync("N08"),
                note = "Thực ra là lỗi tóc",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();

        body.GetProperty("typeCode").GetString().Should().Be("N08");
        body.GetProperty("points").GetInt32().Should().Be(-10);
        body.GetProperty("remediationStatus").GetString().Should().Be(nameof(RemediationStatus.PENDING));
        body.GetProperty("remediationDeadline").GetString().Should().Be("2026-09-10");

        var log = await LastLogAsync(id);

        log.Action.Should().Be(AuditAction.UPDATE);
        log.BeforeJson!.RootElement.GetProperty("TypeCodeSnapshot").GetString().Should().Be("N01");
        log.AfterJson!.RootElement.GetProperty("TypeCodeSnapshot").GetString().Should().Be("N08");
        log.BeforeJson.RootElement.GetProperty("PointsSnapshot").GetInt32().Should().Be(-5);
        log.AfterJson.RootElement.GetProperty("PointsSnapshot").GetInt32().Should().Be(-10);
    }

    [Fact]
    public async Task Moving_a_record_to_another_day_moves_it_into_that_days_week()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        var body = await (await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}",
            new { occurredDate = "2026-09-04" })).ReadJsonAsync();

        body.GetProperty("weekId").GetInt32().Should().Be(fixture.PreviousWeekId);
        body.GetProperty("occurredDate").GetString().Should().Be("2026-09-04");
    }

    [Fact]
    public async Task Deleting_a_record_keeps_the_row_and_takes_it_out_of_every_listing()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        var response = await teacher.DeleteAsync($"/api/classes/{fixture.ClassId}/violations/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listed = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/violations")).ReadJsonAsync();

        listed.GetProperty("items").EnumerateArray()
            .Should().NotContain(r => r.GetProperty("id").GetInt32() == id);

        // PRINCIPLE 6: soft delete. The row is still there for the audit trail.
        await using var db = _factory.CreateDbContext();

        var row = await db.ViolationRecords.IgnoreQueryFilters().SingleAsync(r => r.Id == id);

        row.DeletedAt.Should().NotBeNull();
        row.DeletedBy.Should().Be(fixture.School.Teacher91Id);

        var log = await LastLogAsync(id);

        log.Action.Should().Be(AuditAction.DELETE, "a soft delete is a delete in business terms");
    }

    [Fact]
    public async Task A_locked_week_refuses_every_change_to_its_records()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var id = await ReportAsync(fixture, "N01");

        await TestClassData.LockWeekAsync(_factory, fixture.CurrentWeekId);

        var approve = await teacher.PostAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/approve",
            content: null);

        var edit = await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}",
            new { note = "Sửa sau khi chốt" });

        var delete = await teacher.DeleteAsync($"/api/classes/{fixture.ClassId}/violations/{id}");

        approve.StatusCode.Should().Be(HttpStatusCode.Conflict);
        edit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        delete.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await approve.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("WEEK_LOCKED");
    }

    [Fact]
    public async Task A_class_officer_cannot_approve_anything()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var id = await ReportAsync(fixture, "N01");

        var approve = await officer.PostAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}/approve",
            content: null);

        var delete = await officer.DeleteAsync($"/api/classes/{fixture.ClassId}/violations/{id}");

        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_class_officer_only_ever_sees_the_records_they_entered()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();
        var officer = await fixture.SignInOfficerAsync();

        var mine = await ReportAsync(fixture, "N01");

        var theirs = await (await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("C10"),
                occurredDate = "2026-09-07",
            })).ReadJsonAsync();

        var listed = await (await officer.GetAsync(
            $"/api/classes/{fixture.ClassId}/violations")).ReadJsonAsync();

        var ids = listed.GetProperty("items").EnumerateArray()
            .Select(r => r.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(mine);
        ids.Should().NotContain(theirs.GetProperty("id").GetInt32());
    }

    /// <summary>Files a pending record as the class monitor, and returns its id.</summary>
    private async Task<int> ReportAsync(ClassFixture fixture, string code)
    {
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync(code),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.ReadJsonAsync()).GetProperty("id").GetInt32();
    }

    private async Task<Domain.Entities.AuditLog> LastLogAsync(int recordId)
    {
        await using var db = _factory.CreateDbContext();

        return await db.AuditLogs
            .Where(l => l.Entity == "violation_records" && l.EntityId == recordId.ToString())
            .OrderByDescending(l => l.Id)
            .FirstAsync();
    }
}
