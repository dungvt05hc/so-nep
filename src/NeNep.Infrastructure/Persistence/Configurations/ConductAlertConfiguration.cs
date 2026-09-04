using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ConductAlertConfiguration : IEntityTypeConfiguration<ConductAlert>
{
    public void Configure(EntityTypeBuilder<ConductAlert> builder)
    {
        builder.ToTable("conduct_alerts");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.StudentId, x.TermId, x.RuleId }).IsUnique();
        builder.HasIndex(x => new { x.ClassId, x.TermId, x.Status });
        builder.HasIndex(x => new { x.Status, x.FirstDetectedAt });

        builder.Property(x => x.SuggestedLevels).HasDefaultValue(1);
        builder.Property(x => x.Status).HasDefaultValue(AlertStatus.TRIGGERED);
        builder.Property(x => x.FirstDetectedAt).HasDefaultValueSql("now()");

        // @updatedAt — stamped by TimestampInterceptor on SaveChanges.
        builder.Property(x => x.LastCheckedAt).HasAnnotation(NeNepAnnotations.UpdatedAt, true);

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Term)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Rule)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Adjustment)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.AdjustmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
