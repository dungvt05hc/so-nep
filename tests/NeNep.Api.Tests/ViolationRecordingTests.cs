using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests;

/// <summary>
/// THE ACCEPTANCE TEST OF PHASE 2, entry side: every guard that stands between a phone
/// in a classroom and a row in <c>violation_records</c>.
/// <para>
/// One test per rule, because each of them exists for a reason the school can name, and
/// a rule that quietly stops working must fail here rather than in a report in May.
/// </para>
/// </summary>
[Collection(ApiCollection.Name)]
public class ViolationRecordingTests
{
    private readonly ApiFactory _factory;

    public ViolationRecordingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_class_officer_files_a_record_that_waits_for_the_homeroom_teacher()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
                periodNo = 1,
                note = "Vào lớp trễ 10 phút",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.ReadJsonAsync();

        body.GetProperty("status").GetString().Should().Be(nameof(ViolationStatus.PENDING));
        body.GetProperty("weekId").GetInt32().Should().Be(fixture.CurrentWeekId);
        body.GetProperty("weekNo").GetInt32().Should().Be(2);
        body.GetProperty("isFlagged").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task The_record_snapshots_the_code_name_and_points_of_the_moment()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var body = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            }));

        body.GetProperty("typeCode").GetString().Should().Be("N01");
        body.GetProperty("typeName").GetString().Should().Be("Đi học trễ, vào lớp trễ sau giờ ra chơi");
        body.GetProperty("points").GetInt32().Should().Be(-5);

        // PRINCIPLE 2: the school edits the catalog; the record does not move.
        var id = body.GetProperty("id").GetInt32();

        await using (var db = _factory.CreateDbContext())
        {
            var type = await db.ViolationTypes.SingleAsync(t => t.Code == "N01");

            type.Points = -50;
            type.Name = "Đi học trễ (đã sửa)";

            await db.SaveChangesAsync();
        }

        try
        {
            var after = await (await teacher.GetAsync(
                $"/api/classes/{fixture.ClassId}/violations?studentId={fixture.StudentId}")).ReadJsonAsync();

            var record = after.GetProperty("items").EnumerateArray()
                .Single(r => r.GetProperty("id").GetInt32() == id);

            record.GetProperty("points").GetInt32().Should().Be(-5);
            record.GetProperty("typeName").GetString().Should().Be("Đi học trễ, vào lớp trễ sau giờ ra chơi");
        }
        finally
        {
            await using var db = _factory.CreateDbContext();

            var type = await db.ViolationTypes.SingleAsync(t => t.Code == "N01");

            type.Points = -5;
            type.Name = "Đi học trễ, vào lớp trễ sau giờ ra chơi";

            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData("C10")]
    [InlineData("C12")]
    [InlineData("N12")]
    [InlineData("H11")]
    public async Task A_class_officer_cannot_use_a_code_reserved_for_the_homeroom_teacher(string code)
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync(code),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.ReadJsonAsync();

        body.GetProperty("code").GetString().Should().Be("VIOLATION_TYPE_NOT_ALLOWED_FOR_ROLE");
    }

    [Theory]
    [InlineData("C06")]
    [InlineData("C07")]
    public async Task Nobody_enters_the_system_computed_bonuses_by_hand(string code)
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync(code),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString()
            .Should().Be("VIOLATION_TYPE_AUTO_COMPUTED");
    }

    [Fact]
    public async Task A_code_capped_at_once_a_week_is_refused_the_second_time()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var payload = new
        {
            studentId = fixture.StudentId,
            typeId = await fixture.TypeIdAsync("C12"),
            occurredDate = "2026-09-07",
        };

        (await teacher.PostAsJsonAsync($"/api/classes/{fixture.ClassId}/violations", payload))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await teacher.PostAsJsonAsync($"/api/classes/{fixture.ClassId}/violations", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("MAX_PER_WEEK_EXCEEDED");

        // Another week has its own allowance.
        (await teacher.PostAsJsonAsync(
                $"/api/classes/{fixture.ClassId}/violations",
                new { payload.studentId, payload.typeId, occurredDate = "2026-09-04" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_rejected_record_does_not_use_up_the_weekly_allowance()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();
        var officer = await fixture.SignInOfficerAsync();

        var typeId = await fixture.TypeIdAsync("C01");

        // C01 has no weekly cap, so the check is done on B01, which is capped at one.
        var capped = await fixture.TypeIdAsync("B01");

        var first = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new { studentId = fixture.StudentId, typeId = capped, occurredDate = "2026-09-07" }));

        // A record entered by the homeroom teacher is approved on the spot, so it has to
        // be a pending one that gets rejected: enter it as the class officer.
        var pending = await Created(await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new { studentId = fixture.OtherStudentId, typeId, occurredDate = "2026-09-07" }));

        (await teacher.PostAsJsonAsync(
                $"/api/classes/{fixture.ClassId}/violations/{pending.GetProperty("id").GetInt32()}/reject",
                new { reason = "Ghi nhầm học sinh" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        first.GetProperty("status").GetString().Should().Be(nameof(ViolationStatus.APPROVED));
    }

    [Fact]
    public async Task A_class_officer_cannot_award_bonus_points_to_themselves()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OfficerStudentId,
                typeId = await fixture.TypeIdAsync("C01"),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("SELF_AWARDED_BONUS");
    }

    [Fact]
    public async Task A_class_officer_may_still_record_their_own_violation()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OfficerStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadJsonAsync()).GetProperty("isFlagged").GetBoolean()
            .Should().BeFalse("a pupil recording themselves needs no extra scrutiny");
    }

    [Fact]
    public async Task One_class_officer_recording_another_is_flagged_for_the_homeroom_teacher()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);

        // The other pupil is an officer too, so this is officer against officer.
        await TestClassData.AddOfficerAppointmentAsync(
            _factory,
            fixture.ClassId,
            fixture.OtherStudentId,
            Role.PHO_HOC_TAP);

        var officer = await fixture.SignInOfficerAsync();

        var body = await Created(await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            }));

        body.GetProperty("isFlagged").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task A_record_entered_by_the_homeroom_teacher_is_never_flagged()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);

        await TestClassData.AddOfficerAppointmentAsync(_factory, fixture.ClassId, fixture.OtherStudentId);

        var teacher = await fixture.SignInTeacherAsync();

        var body = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            }));

        body.GetProperty("isFlagged").GetBoolean().Should().BeFalse();
        body.GetProperty("status").GetString().Should().Be(nameof(ViolationStatus.APPROVED));
    }

    [Fact]
    public async Task A_code_that_requires_remediation_gets_its_deadline_from_the_catalog()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        // N08 (hair) allows 3 days; V01 (missed cleaning duty) allows a week.
        var hair = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N08"),
                occurredDate = "2026-09-04",
            }));

        hair.GetProperty("remediationStatus").GetString().Should().Be(nameof(RemediationStatus.PENDING));
        hair.GetProperty("remediationNote").GetString().Should().Be("Chỉnh sửa tóc đúng quy định");
        hair.GetProperty("remediationDeadline").GetString().Should().Be("2026-09-07");

        var duty = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("V01"),
                occurredDate = "2026-09-04",
            }));

        duty.GetProperty("remediationDeadline").GetString().Should().Be("2026-09-11");

        // A code without a remediation requirement carries none.
        var late = await Created(await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-04",
            }));

        late.GetProperty("remediationStatus").GetString().Should().Be(nameof(RemediationStatus.NOT_REQUIRED));
        late.GetProperty("remediationDeadline").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Nothing_can_be_written_into_a_week_that_is_already_locked()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        await TestClassData.LockWeekAsync(_factory, fixture.CurrentWeekId);

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("WEEK_LOCKED");
    }

    [Fact]
    public async Task A_date_that_belongs_to_no_academic_week_is_refused()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-08-17",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("NO_ACADEMIC_WEEK");
    }

    [Fact]
    public async Task A_record_cannot_be_dated_in_the_future()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        // The test clock stands at 08:00 Vietnam time on Monday 7 September 2026.
        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-08",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_pupil_of_another_class_cannot_be_recorded_through_this_class()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.School.Student92Id,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_code_switched_off_for_the_class_cannot_be_used()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var typeId = await fixture.TypeIdAsync("V06");

        (await teacher.PutAsJsonAsync(
                $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
                new { isEnabled = false, note = "Lớp học ở phòng bộ môn, không áp dụng" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new { studentId = fixture.StudentId, typeId, occurredDate = "2026-09-07" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString()
            .Should().Be("VIOLATION_TYPE_DISABLED_FOR_CLASS");
    }

    private static async Task<System.Text.Json.JsonElement> Created(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await response.ReadJsonAsync();
    }
}
