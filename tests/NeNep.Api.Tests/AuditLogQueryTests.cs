using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Tests;

/// <summary>
/// The change-log screen: every write on a record is there with its before and after,
/// and the trail is fenced by the same class boundary as the data it describes.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuditLogQueryTests
{
    private readonly ApiFactory _factory;

    public AuditLogQueryTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_edit_approve_and_delete_all_show_up_for_one_record()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();
        var officer = await fixture.SignInOfficerAsync();

        var created = await (await officer.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.OtherStudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            })).ReadJsonAsync();

        var id = created.GetProperty("id").GetInt32();

        await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations/{id}",
            new { note = "Trễ 15 phút" });

        await teacher.PostAsync($"/api/classes/{fixture.ClassId}/violations/{id}/approve", content: null);
        await teacher.DeleteAsync($"/api/classes/{fixture.ClassId}/violations/{id}");

        var body = await (await teacher.GetAsync(
            $"/api/audit-logs?entity=violation_records&entityId={id}")).ReadJsonAsync();

        var actions = body.GetProperty("items").EnumerateArray()
            .Select(l => l.GetProperty("action").GetString())
            .ToList();

        actions.Should().Equal(
            nameof(AuditAction.DELETE),
            nameof(AuditAction.APPROVE),
            nameof(AuditAction.UPDATE),
            nameof(AuditAction.CREATE));

        var create = body.GetProperty("items").EnumerateArray()
            .Last();

        create.GetProperty("before").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        create.GetProperty("after").GetProperty("TypeCodeSnapshot").GetString().Should().Be("N01");
        create.GetProperty("actorId").GetInt32().Should().Be(
            await OfficerUserIdAsync(fixture),
            "the record was entered by the class monitor");
        create.GetProperty("actorRole").GetString().Should().Be(nameof(Role.LOP_TRUONG));

        var update = body.GetProperty("items").EnumerateArray()
            .Single(l => l.GetProperty("action").GetString() == nameof(AuditAction.UPDATE));

        update.GetProperty("before").GetProperty("Note").ValueKind
            .Should().Be(System.Text.Json.JsonValueKind.Null);
        update.GetProperty("after").GetProperty("Note").GetString().Should().Be("Trễ 15 phút");
        update.GetProperty("classId").GetInt32().Should().Be(fixture.ClassId);
        update.GetProperty("classCode").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task A_homeroom_teacher_reads_the_trail_of_their_own_class_only()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher91 = await fixture.SignInTeacherAsync();

        await teacher91.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            });

        var refused = await teacher91.GetAsync($"/api/audit-logs?classId={fixture.School.Class92Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var mine = await (await teacher91.GetAsync("/api/audit-logs")).ReadJsonAsync();

        mine.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(l => l.GetProperty("classId").GetInt32() == fixture.ClassId);
    }

    [Fact]
    public async Task The_school_board_reads_the_whole_school_and_filters_by_action()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var username = $"bgh-{fixture.School.Token}";

        await TestUsers.AddSchoolWideAsync(_factory, username, Role.BGH);

        var created = await (await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            })).ReadJsonAsync();

        var board = await _factory.SignInAsync(username, TestSchool.Password);

        var body = await (await board.GetAsync(
            $"/api/audit-logs?entity=violation_records&action={nameof(AuditAction.CREATE)}&classId={fixture.ClassId}"))
            .ReadJsonAsync();

        body.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(l => l.GetProperty("action").GetString() == nameof(AuditAction.CREATE));

        body.GetProperty("items").EnumerateArray()
            .Select(l => l.GetProperty("entityId").GetString())
            .Should().Contain(created.GetProperty("id").GetInt32().ToString());
    }

    [Fact]
    public async Task The_trail_cannot_be_edited_or_deleted_even_from_inside_the_system()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        await teacher.PostAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violations",
            new
            {
                studentId = fixture.StudentId,
                typeId = await fixture.TypeIdAsync("N01"),
                occurredDate = "2026-09-07",
            });

        // A context built from the application's own services, so the interceptor that
        // guards the trail is in place.
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<NeNepDbContext>();

        var log = db.AuditLogs.OrderByDescending(l => l.Id).First();

        log.Summary = "Sửa nhật ký";

        var editing = () => db.SaveChangesAsync();

        await editing.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    private async Task<int> OfficerUserIdAsync(ClassFixture fixture)
    {
        await using var db = _factory.CreateDbContext();

        return db.Users.Single(u => u.Username == fixture.OfficerUsername).Id;
    }
}
