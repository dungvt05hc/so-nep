using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests;

/// <summary>
/// DECISION Q7: one catalog for the whole school, which a homeroom teacher may switch
/// codes off in — and may not re-price while the school keeps that door shut.
/// </summary>
[Collection(ApiCollection.Name)]
public class ClassCatalogTests
{
    private readonly ApiFactory _factory;

    public ClassCatalogTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_class_catalog_starts_as_the_school_catalog()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var body = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/violation-types")).ReadJsonAsync();

        body.GetArrayLength().Should().Be(49);

        var n01 = body.EnumerateArray().Single(t => t.GetProperty("code").GetString() == "N01");

        n01.GetProperty("isEnabled").GetBoolean().Should().BeTrue();
        n01.GetProperty("points").GetInt32().Should().Be(-5);
        n01.GetProperty("effectivePoints").GetInt32().Should().Be(-5);
        n01.GetProperty("pointsOverride").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task A_homeroom_teacher_can_switch_a_code_off_and_back_on_for_their_class()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var typeId = await fixture.TypeIdAsync("V07");

        var off = await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
            new { isEnabled = false, note = "Lớp ăn trưa tại lớp theo lịch bán trú" });

        off.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await off.ReadJsonAsync();

        body.GetProperty("isEnabled").GetBoolean().Should().BeFalse();
        body.GetProperty("overrideNote").GetString().Should().Be("Lớp ăn trưa tại lớp theo lịch bán trú");

        // The switch is recorded as a catalog change, not as an anonymous update.
        await using (var db = _factory.CreateDbContext())
        {
            var log = await db.AuditLogs
                .Where(l => l.Entity == "class_violation_overrides" && l.ClassId == fixture.ClassId)
                .OrderByDescending(l => l.Id)
                .FirstAsync();

            log.Action.Should().Be(AuditAction.OVERRIDE_CATALOG);
            log.Summary.Should().Be("Tắt mã lỗi V07 cho lớp");
        }

        var on = await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
            new { isEnabled = true });

        (await on.ReadJsonAsync()).GetProperty("isEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Switching_a_code_off_leaves_the_other_62_classes_alone()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher91 = await fixture.SignInTeacherAsync();
        var teacher92 = await _factory.SignInAsync(fixture.School.Teacher92Username, TestSchool.Password);

        var typeId = await fixture.TypeIdAsync("V06");

        await teacher91.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
            new { isEnabled = false });

        var other = await (await teacher92.GetAsync(
            $"/api/classes/{fixture.School.Class92Id}/violation-types")).ReadJsonAsync();

        other.EnumerateArray()
            .Single(t => t.GetProperty("code").GetString() == "V06")
            .GetProperty("isEnabled").GetBoolean()
            .Should().BeTrue();
    }

    [Fact]
    public async Task A_homeroom_teacher_cannot_re_price_a_code_while_the_school_forbids_it()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var typeId = await fixture.TypeIdAsync("N01");

        var response = await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
            new { isEnabled = true, pointsOverride = -1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.ReadJsonAsync()).GetProperty("code").GetString()
            .Should().Be("CLASS_POINT_OVERRIDE_DISABLED");

        // Nothing was stored, so nothing can leak into a score later.
        await using var db = _factory.CreateDbContext();

        (await db.ClassViolationOverrides.AnyAsync(o => o.ClassId == fixture.ClassId && o.TypeId == typeId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task With_the_school_flag_on_the_class_price_is_what_the_record_snapshots()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var typeId = await fixture.TypeIdAsync("H01");

        await SetPointOverrideAllowedAsync(true);

        try
        {
            var response = await teacher.PutAsJsonAsync(
                $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
                new { isEnabled = true, pointsOverride = -8, note = "Lớp chuyên, siết kỷ luật giờ học" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.ReadJsonAsync();

            body.GetProperty("points").GetInt32().Should().Be(-5, "the school value does not move");
            body.GetProperty("effectivePoints").GetInt32().Should().Be(-8);

            var record = await (await teacher.PostAsJsonAsync(
                $"/api/classes/{fixture.ClassId}/violations",
                new { studentId = fixture.StudentId, typeId, occurredDate = "2026-09-07" })).ReadJsonAsync();

            record.GetProperty("points").GetInt32().Should().Be(-8);
        }
        finally
        {
            await SetPointOverrideAllowedAsync(false);
        }
    }

    [Fact]
    public async Task Turning_the_school_flag_back_off_puts_the_school_price_back_in_charge()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var teacher = await fixture.SignInTeacherAsync();

        var typeId = await fixture.TypeIdAsync("H02");

        await SetPointOverrideAllowedAsync(true);

        await teacher.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{typeId}",
            new { isEnabled = true, pointsOverride = -3 });

        await SetPointOverrideAllowedAsync(false);

        var body = await (await teacher.GetAsync(
            $"/api/classes/{fixture.ClassId}/violation-types")).ReadJsonAsync();

        var h02 = body.EnumerateArray().Single(t => t.GetProperty("code").GetString() == "H02");

        // The adjustment is still on file, but it is not what counts any more.
        h02.GetProperty("pointsOverride").GetInt32().Should().Be(-3);
        h02.GetProperty("effectivePoints").GetInt32().Should().Be(-10);
    }

    [Fact]
    public async Task A_class_officer_cannot_change_the_catalog()
    {
        var fixture = await ClassFixture.CreateAsync(_factory);
        var officer = await fixture.SignInOfficerAsync();

        var response = await officer.PutAsJsonAsync(
            $"/api/classes/{fixture.ClassId}/violation-types/{await fixture.TypeIdAsync("N01")}",
            new { isEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SetPointOverrideAllowedAsync(bool allowed)
    {
        await using var db = _factory.CreateDbContext();

        var settings = await db.SchoolSettings.SingleAsync();

        settings.AllowClassPointOverride = allowed;

        await db.SaveChangesAsync();
    }
}
