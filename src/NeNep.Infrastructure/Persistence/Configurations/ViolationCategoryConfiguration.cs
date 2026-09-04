using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ViolationCategoryConfiguration : IEntityTypeConfiguration<ViolationCategory>
{
    public void Configure(EntityTypeBuilder<ViolationCategory> builder)
    {
        builder.ToTable("violation_categories");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Kind).IsUnique();

        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}
