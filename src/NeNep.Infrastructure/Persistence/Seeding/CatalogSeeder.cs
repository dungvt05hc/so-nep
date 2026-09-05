using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Domain.Seeding;

namespace NeNep.Infrastructure.Persistence.Seeding;

/// <summary>
/// Writes <see cref="CatalogSeedData"/> into the database. Safe to run on every start:
/// rows are matched by their business code and only the differing columns are updated,
/// so a restart that changes nothing produces no writes and no audit rows.
/// <para>
/// The integrity check runs FIRST. A catalog that does not hold together must never
/// reach the database, because a wrong point value is only noticed months later, in a
/// report already sent to parents.
/// </para>
/// </summary>
public sealed class CatalogSeeder
{
    private readonly NeNepDbContext _db;
    private readonly ILogger<CatalogSeeder> _logger;

    public CatalogSeeder(NeNepDbContext db, ILogger<CatalogSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var errors = CatalogSeedValidator.Validate();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "The catalog seed data is inconsistent and was not written:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        }

        await EnsureSettingsAsync(cancellationToken);
        await SeedLevelsAsync(cancellationToken);

        var categoryIds = await SeedCategoriesAsync(cancellationToken);

        await SeedTypesAsync(categoryIds, cancellationToken);
        await SeedRulesAsync(cancellationToken);

        var written = await _db.SaveChangesAsync(cancellationToken);

        if (written > 0)
        {
            _logger.LogInformation("Catalog seed applied: {Rows} rows written.", written);
        }
    }

    /// <summary>
    /// Creates the single configuration row if it is missing, and leaves it alone
    /// afterwards — the school edits it from the configuration screen.
    /// <para>
    /// PRINCIPLE 1: not one regulation number is written here. Each column is filled from
    /// the default declared in <c>SchoolSettingConfiguration</c>, which is the only place
    /// those numbers are allowed to appear.
    /// </para>
    /// <para>
    /// The values are copied onto the entity rather than left to the database, because EF
    /// only omits a column from the INSERT while the property still holds its sentinel —
    /// and for a <c>bool</c> defaulting to true the sentinel is <c>true</c>, so a freshly
    /// constructed entity would write <c>false</c> over the default.
    /// </para>
    /// </summary>
    private async Task EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _db.SchoolSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        var settings = new SchoolSetting();

        foreach (var property in _db.Model.FindEntityType(typeof(SchoolSetting))!.GetProperties())
        {
            if (property.GetDefaultValue() is { } value && property.PropertyInfo is { } info)
            {
                info.SetValue(settings, value);
            }
        }

        settings.SchoolName = CatalogSeedData.DefaultSchoolName;

        _db.SchoolSettings.Add(settings);
    }

    private async Task SeedLevelsAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.ClassificationLevels.ToDictionaryAsync(l => l.Code, cancellationToken);

        foreach (var seed in CatalogSeedData.ClassificationLevels)
        {
            if (!existing.TryGetValue(seed.Code, out var level))
            {
                // IsActive is set here and never again: see the note on SeedTypesAsync.
                level = new ClassificationLevel { Code = seed.Code, IsActive = true };
                _db.ClassificationLevels.Add(level);
            }

            level.Name = seed.Name;
            level.RankOrder = seed.RankOrder;
            level.MinScore = seed.MinScore;
            level.MaxScore = seed.MaxScore;
            level.Color = seed.Color;
        }
    }

    /// <summary>Returns the category id of each <see cref="CategoryKind"/>, for the codes.</summary>
    private async Task<Dictionary<CategoryKind, ViolationCategory>> SeedCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var existing = await _db.ViolationCategories.ToDictionaryAsync(c => c.Kind, cancellationToken);

        foreach (var seed in CatalogSeedData.Categories)
        {
            if (!existing.TryGetValue(seed.Kind, out var category))
            {
                category = new ViolationCategory { Kind = seed.Kind, IsActive = true };
                _db.ViolationCategories.Add(category);
                existing[seed.Kind] = category;
            }

            category.Name = seed.Name;
            category.Ordinal = seed.Ordinal;
        }

        return existing;
    }

    /// <summary>
    /// Loads the 49 codes.
    /// <para>
    /// <c>IsActive</c> is set when the row is CREATED and never touched again: a code the
    /// administrator retired must stay retired, or every restart would put it back in
    /// front of 62 classes. Everything else is reference data and is refreshed.
    /// </para>
    /// </summary>
    private async Task SeedTypesAsync(
        Dictionary<CategoryKind, ViolationCategory> categories,
        CancellationToken cancellationToken)
    {
        // Class-specific codes a homeroom teacher added are not part of the school
        // catalog and must never be touched by the seeder.
        var existing = await _db.ViolationTypes
            .Where(t => t.ClassId == null)
            .ToDictionaryAsync(t => t.Code, cancellationToken);

        var ordinal = 0;

        foreach (var seed in CatalogSeedData.ViolationTypes)
        {
            ordinal++;

            if (!existing.TryGetValue(seed.Code, out var type))
            {
                type = new ViolationType { Code = seed.Code, IsActive = true };
                _db.ViolationTypes.Add(type);
            }

            type.Category = categories[seed.Kind];
            type.Name = seed.Name;
            type.Points = seed.Points;
            type.CountsForScore = seed.CountsForScore;
            type.AllowedRoles = [.. seed.AllowedRoles];
            type.IsBulkCapable = seed.IsBulkCapable;
            type.IsAutoComputed = seed.IsAutoComputed;
            type.MaxPerWeek = seed.MaxPerWeek;
            type.RequiresRemediation = seed.RequiresRemediation;
            type.RemediationNote = seed.RemediationNote;
            type.RemediationDays = seed.RemediationDays;
            type.Ordinal = ordinal;
            type.Note = seed.Note;
        }
    }

    private async Task SeedRulesAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.ConductRules.ToDictionaryAsync(r => r.Code, cancellationToken);

        foreach (var seed in CatalogSeedData.ConductRules)
        {
            if (!existing.TryGetValue(seed.Code, out var rule))
            {
                rule = new ConductRule { Code = seed.Code, IsActive = true };
                _db.ConductRules.Add(rule);
            }

            rule.Name = seed.Name;
            rule.RuleType = seed.RuleType;
            rule.ViolationCodes = [.. seed.ViolationCodes];
            rule.Operator = seed.Operator;
            rule.ThresholdValue = seed.ThresholdValue;
            rule.PeriodScope = seed.PeriodScope;
            rule.Effect = seed.Effect;
            rule.EffectLevels = seed.EffectLevels;
            rule.Note = seed.Note;
        }
    }
}
