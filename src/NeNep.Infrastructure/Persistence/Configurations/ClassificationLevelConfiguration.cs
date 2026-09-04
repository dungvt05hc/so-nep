using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ClassificationLevelConfiguration : IEntityTypeConfiguration<ClassificationLevel>
{
    public void Configure(EntityTypeBuilder<ClassificationLevel> builder)
    {
        builder.ToTable("classification_levels");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.RankOrder).IsUnique();

        builder.Property(x => x.MinScore).HasPrecision(6, 2);
        builder.Property(x => x.MaxScore).HasPrecision(6, 2);
        builder.Property(x => x.Color).HasDefaultValue("#888888");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}
