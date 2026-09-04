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

        builder.Property(x => x.Operator).HasDefaultValue(CompareOp.GT);
        builder.Property(x => x.PeriodScope).HasDefaultValue(PeriodScope.TERM);
        builder.Property(x => x.EffectLevels).HasDefaultValue(1);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.EffectiveFrom).HasDefaultValueSql("now()");
    }
}
