using FluentAssertions;
using NeNep.Domain.Enums;
using NeNep.Domain.Seeding;

namespace NeNep.Scoring.Tests;

/// <summary>
/// The integrity check of the source data, run without a database — the C# counterpart
/// of <c>validateSeedData()</c> in <c>docs/seed-data.ts</c>.
/// <para>
/// A wrong point value or a rule pointing at a code that does not exist would only be
/// noticed months later, in a report already sent home, so it is caught here first.
/// </para>
/// </summary>
public class CatalogSeedDataTests
{
    [Fact]
    public void The_catalog_holds_together()
    {
        var errors = CatalogSeedValidator.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void The_catalog_has_the_49_codes_of_the_regulations()
    {
        // 47 codes in the regulations plus B01 and B02.
        CatalogSeedData.ViolationTypes.Should().HaveCount(49);

        CatalogSeedData.ViolationTypes
            .Count(t => t.Kind == CategoryKind.KHEN_THUONG)
            .Should().Be(14, "12 bonus codes plus the two upgrade records");

        CatalogSeedData.ViolationTypes.Count(t => t.Kind == CategoryKind.NE_NEP).Should().Be(15);
        CatalogSeedData.ViolationTypes.Count(t => t.Kind == CategoryKind.VE_SINH).Should().Be(7);
        CatalogSeedData.ViolationTypes.Count(t => t.Kind == CategoryKind.HOC_TAP).Should().Be(13);
    }

    [Fact]
    public void There_are_4_groups_14_rules_and_4_classification_levels()
    {
        CatalogSeedData.Categories.Should().HaveCount(4);
        CatalogSeedData.ConductRules.Should().HaveCount(14);
        CatalogSeedData.ClassificationLevels.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("C01", 2)]
    [InlineData("C03", 20)]
    [InlineData("N01", -5)]
    [InlineData("N12", -30)]
    [InlineData("H10", -25)]
    [InlineData("V05", -20)]
    public void Points_keep_their_sign_from_the_regulations(string code, int points) =>
        Find(code).Points.Should().Be(points);

    [Fact]
    public void The_codes_that_are_only_tracked_never_reach_the_score()
    {
        // N04 (excused absence) and B01/B02 are recorded but never scored.
        Find("N04").CountsForScore.Should().BeFalse();
        Find("B01").CountsForScore.Should().BeFalse();
        Find("B02").CountsForScore.Should().BeFalse();
    }

    [Fact]
    public void The_two_system_computed_bonuses_cannot_be_entered_by_hand()
    {
        foreach (var code in new[] { "C06", "C07" })
        {
            var type = Find(code);

            type.IsAutoComputed.Should().BeTrue();
            type.AllowedRoles.Should().BeEmpty();
            type.MaxPerWeek.Should().Be(1);
        }
    }

    [Fact]
    public void The_codes_reserved_for_the_homeroom_teacher_stay_reserved()
    {
        foreach (var code in new[] { "C02", "C03", "C10", "C12", "N11", "N12", "N13", "V05", "H11", "B01", "B02" })
        {
            Find(code).AllowedRoles.Should().Equal([Role.GVCN], "{0} is a homeroom-teacher code", code);
        }
    }

    [Fact]
    public void Every_code_that_needs_remediation_says_how_long_it_has()
    {
        var needing = CatalogSeedData.ViolationTypes.Where(t => t.RequiresRemediation).ToList();

        needing.Should().HaveCount(8);
        needing.Should().OnlyContain(t => t.RemediationDays != null && t.RemediationNote != null);

        Find("N08").RemediationDays.Should().Be(3);
        Find("N09").RemediationDays.Should().Be(0, "make-up is immediate");
        Find("V01").RemediationDays.Should().Be(7);
    }

    [Fact]
    public void Every_rule_points_at_codes_that_exist_and_carries_its_threshold()
    {
        var codes = CatalogSeedData.ViolationTypes.Select(t => t.Code).ToHashSet();

        CatalogSeedData.ConductRules
            .SelectMany(r => r.ViolationCodes)
            .Should().OnlyContain(c => codes.Contains(c));

        // R02 groups the two absence codes under one threshold.
        var r02 = CatalogSeedData.ConductRules.Single(r => r.Code == "R02");

        r02.ViolationCodes.Should().Equal("N02", "N03");
        r02.Operator.Should().Be(CompareOp.GT);
        r02.ThresholdValue.Should().Be(3);
        r02.PeriodScope.Should().Be(PeriodScope.TERM);
    }

    [Fact]
    public void No_rule_applies_itself_the_app_only_proposes()
    {
        // DECISION Q3 + Q4: everything here is a PROPOSAL for the review meeting. The
        // only rule that is not about classification is the year-long absence warning.
        CatalogSeedData.ConductRules
            .Where(r => r.Effect == RuleEffect.WARNING)
            .Should().ContainSingle(r => r.Code == "R14");

        CatalogSeedData.ConductRules
            .Where(r => r.Effect != RuleEffect.WARNING)
            .Should().OnlyContain(r => r.EffectLevels == 1);
    }

    [Fact]
    public void The_classification_bands_tile_the_scale_without_a_gap()
    {
        var levels = CatalogSeedData.ClassificationLevels.OrderBy(l => l.RankOrder).ToList();

        levels.Select(l => l.Code).Should().Equal("CHUA_DAT", "DAT", "KHA", "TOT");

        levels[0].MinScore.Should().BeNull();
        levels[^1].MaxScore.Should().BeNull();

        levels[0].MaxScore.Should().Be(30);
        levels[1].MaxScore.Should().Be(60);
        levels[2].MaxScore.Should().Be(90);
    }

    private static SeedViolationType Find(string code) =>
        CatalogSeedData.ViolationTypes.Single(t => t.Code == code);
}
