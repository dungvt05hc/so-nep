using NeNep.Domain.Enums;

namespace NeNep.Domain.Seeding;

/// <summary>
/// Integrity check of <see cref="CatalogSeedData"/> — the port of <c>validateSeedData()</c>
/// in <c>docs/seed-data.ts</c>. It runs without a database, so both the unit tests and
/// the seeder can call it before a single row is written.
/// <para>
/// The messages are developer diagnostics, not user-facing text, so they are in English.
/// </para>
/// </summary>
public static class CatalogSeedValidator
{
    /// <summary>Returns one message per problem found; an empty list means the data is sound.</summary>
    public static IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var codes = new HashSet<string>();

        foreach (var type in CatalogSeedData.ViolationTypes)
        {
            if (!codes.Add(type.Code))
            {
                errors.Add($"Duplicate violation code: {type.Code}");
            }

            if (type.CountsForScore && type.Points == 0)
            {
                errors.Add($"{type.Code}: counts towards the score but its points are 0.");
            }

            if (type.Kind == CategoryKind.KHEN_THUONG && type.Points < 0)
            {
                errors.Add($"{type.Code}: is a commendation but carries negative points.");
            }

            if (type.Kind != CategoryKind.KHEN_THUONG && type.Points > 0)
            {
                errors.Add($"{type.Code}: is a violation but carries positive points.");
            }

            if (type.RequiresRemediation && type.RemediationDays is null)
            {
                errors.Add($"{type.Code}: requires remediation but has no remediationDays.");
            }

            if (type.IsAutoComputed && type.AllowedRoles.Count > 0)
            {
                errors.Add($"{type.Code}: is computed by the system but still allows manual entry.");
            }
        }

        var ruleCodes = new HashSet<string>();

        foreach (var rule in CatalogSeedData.ConductRules)
        {
            if (!ruleCodes.Add(rule.Code))
            {
                errors.Add($"Duplicate rule code: {rule.Code}");
            }

            foreach (var code in rule.ViolationCodes.Where(c => !codes.Contains(c)))
            {
                errors.Add($"{rule.Code}: references the unknown violation code \"{code}\".");
            }

            if (rule.RuleType == RuleType.THRESHOLD && (rule.ThresholdValue is null || rule.Operator is null))
            {
                errors.Add($"{rule.Code}: is a threshold rule but has no operator or thresholdValue.");
            }
        }

        errors.AddRange(ValidateLevels());

        return errors;
    }

    /// <summary>
    /// The classification bands must tile the number line: no gap and no overlap between
    /// two neighbouring levels, and open at both ends.
    /// </summary>
    private static IEnumerable<string> ValidateLevels()
    {
        var sorted = CatalogSeedData.ClassificationLevels
            .OrderBy(l => l.RankOrder)
            .ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i].MaxScore != sorted[i + 1].MinScore)
            {
                yield return
                    $"Classification bands leave a gap or overlap between {sorted[i].Code} and {sorted[i + 1].Code}.";
            }
        }

        if (sorted[0].MinScore is not null)
        {
            yield return "The lowest classification level must be open at the bottom.";
        }

        if (sorted[^1].MaxScore is not null)
        {
            yield return "The highest classification level must be open at the top.";
        }
    }
}
