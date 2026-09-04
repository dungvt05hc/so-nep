using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ClassViolationOverrideConfiguration : IEntityTypeConfiguration<ClassViolationOverride>
{
    public void Configure(EntityTypeBuilder<ClassViolationOverride> builder)
    {
        builder.ToTable("class_violation_overrides");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ClassId, x.TypeId }).IsUnique();

        builder.Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        // @updatedAt — stamped by TimestampInterceptor on SaveChanges.
        builder.Property(x => x.UpdatedAt).HasAnnotation(NeNepAnnotations.UpdatedAt, true);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Overrides)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Type)
            .WithMany(x => x.Overrides)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany(x => x.Overrides)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
