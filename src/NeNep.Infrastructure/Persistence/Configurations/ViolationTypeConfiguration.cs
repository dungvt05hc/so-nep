using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ViolationTypeConfiguration : IEntityTypeConfiguration<ViolationType>
{
    public void Configure(EntityTypeBuilder<ViolationType> builder)
    {
        builder.ToTable("violation_types");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.CategoryId, x.IsActive });

        builder.Property(x => x.CountsForScore).HasDefaultValue(true);
        builder.Property(x => x.IsBulkCapable).HasDefaultValue(false);
        builder.Property(x => x.IsAutoComputed).HasDefaultValue(false);
        builder.Property(x => x.RequiresRemediation).HasDefaultValue(false);
        builder.Property(x => x.Ordinal).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Prisma: Role[] — Npgsql maps this straight onto the role[] enum array.
        builder.Property(x => x.AllowedRoles).HasColumnType("role[]");

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Types)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.ViolationTypes)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
