using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence.Seeding;

namespace NeNep.Api.Tests;

/// <summary>
/// The catalog reaching the database intact, with the values from the regulations —
/// and staying intact when the application restarts.
/// </summary>
[Collection(ApiCollection.Name)]
public class CatalogSeedingTests
{
    private readonly ApiFactory _factory;

    public CatalogSeedingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Start_up_loads_the_49_codes_4_groups_14_rules_and_4_levels()
    {
        await using var db = _factory.CreateDbContext();

        (await db.ViolationTypes.CountAsync(t => t.ClassId == null)).Should().Be(49);
        (await db.ViolationCategories.CountAsync()).Should().Be(4);
        (await db.ConductRules.CountAsync()).Should().Be(14);
        (await db.ClassificationLevels.CountAsync()).Should().Be(4);

        // The single configuration row exists, with the defaults of the regulations.
        var settings = await db.SchoolSettings.SingleAsync();

        settings.BaseScore.Should().Be(100);
        settings.AllowClassPointOverride.Should().BeFalse();
        settings.StackAutoBonus.Should().BeTrue();
        settings.ExpirePendingOnLock.Should().BeTrue();
    }

    [Fact]
    public async Task The_columns_that_carry_a_decision_survive_the_round_trip()
    {
        await using var db = _factory.CreateDbContext();

        // N04 is the trap: "false" is also the CLR default, and the column defaults to
        // true in the database. Getting this wrong makes excused absences cost points.
        var n04 = await db.ViolationTypes.SingleAsync(t => t.Code == "N04");

        n04.CountsForScore.Should().BeFalse();
        n04.Points.Should().Be(0);

        var c06 = await db.ViolationTypes.SingleAsync(t => t.Code == "C06");

        c06.IsAutoComputed.Should().BeTrue();
        c06.AllowedRoles.Should().BeEmpty();
        c06.MaxPerWeek.Should().Be(1);

        var n08 = await db.ViolationTypes.SingleAsync(t => t.Code == "N08");

        n08.RequiresRemediation.Should().BeTrue();
        n08.RemediationDays.Should().Be(3);
        n08.AllowedRoles.Should().Contain(Role.LOP_TRUONG);

        var c10 = await db.ViolationTypes.SingleAsync(t => t.Code == "C10");

        c10.AllowedRoles.Should().Equal([Role.GVCN]);

        var r02 = await db.ConductRules.SingleAsync(r => r.Code == "R02");

        r02.ViolationCodes.Should().Equal("N02", "N03");
        r02.Operator.Should().Be(CompareOp.GT);
        r02.ThresholdValue.Should().Be(3);
    }

    [Fact]
    public async Task Running_the_seeder_again_changes_nothing_and_writes_no_audit_row()
    {
        await using var db = _factory.CreateDbContext();

        var lastAuditId = await db.AuditLogs.MaxAsync(l => (long?)l.Id) ?? 0;
        var typesBefore = await db.ViolationTypes.CountAsync(t => t.ClassId == null);

        using var scope = _factory.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync();

        (await db.ViolationTypes.CountAsync(t => t.ClassId == null)).Should().Be(typesBefore);

        var written = await db.AuditLogs
            .Where(l => l.Id > lastAuditId)
            .Select(l => new { l.Entity, l.EntityId, l.Action, l.AfterJson })
            .ToListAsync();

        written
            .Select(l => $"{l.Entity} {l.EntityId} {l.Action} {l.AfterJson?.RootElement}")
            .Should().BeEmpty();
    }

    [Fact]
    public async Task The_catalog_endpoint_returns_the_codes_in_display_order()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var response = await teacher.GetAsync("/api/catalog/violation-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();
        var codes = body.EnumerateArray().Select(t => t.GetProperty("code").GetString()).ToList();

        codes.Should().HaveCount(49);
        codes[0].Should().Be("C01");
        codes[^1].Should().Be("B02");

        var n01 = body.EnumerateArray().Single(t => t.GetProperty("code").GetString() == "N01");

        n01.GetProperty("points").GetInt32().Should().Be(-5);
        n01.GetProperty("kind").GetString().Should().Be(nameof(CategoryKind.NE_NEP));
    }

    [Fact]
    public async Task An_anonymous_caller_gets_nothing_from_the_catalog()
    {
        var response = await _factory.CreateClient().GetAsync("/api/catalog/violation-types");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
