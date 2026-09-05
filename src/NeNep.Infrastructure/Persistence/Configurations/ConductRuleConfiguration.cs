using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ConductRuleConfiguration : IEntityTypeConfiguration<ConductRule>
{
    public void Configure(EntityTypeBuilder<ConductRule> builder)
    {
        builder.ToTable("conduct_rules");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Code).IsUnique();

        // The column defaults to GT, but R04 (remediation timeout) compares nothing and
        // must store NULL. Without a sentinel EF treats "null" as "not set", falls back to
        // the default, and the seeder then rewrites the row on every single start-up.
        builder.Property(x => x.Operator)
            .HasDefaultValue(CompareOp.GT)
            .HasSentinel(CompareOp.GT);

        // Same reason as above: a WEEK-scoped rule and R14's "no level change" both sit on
        // the CLR default of their type, so without a sentinel they would be read as
        // "not set" and silently take the column default instead.
        builder.Property(x => x.PeriodScope)
            .HasDefaultValue(PeriodScope.TERM)
            .HasSentinel(PeriodScope.TERM);

        builder.Property(x => x.EffectLevels)
            .HasDefaultValue(1)
            .HasSentinel(1);

        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.EffectiveFrom).HasDefaultValueSql("now()");
    }
}
