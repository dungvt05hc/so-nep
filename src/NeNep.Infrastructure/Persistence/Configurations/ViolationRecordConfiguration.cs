using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ViolationRecordConfiguration : IEntityTypeConfiguration<ViolationRecord>
{
    public void Configure(EntityTypeBuilder<ViolationRecord> builder)
    {
        builder.ToTable("violation_records");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ClassId, x.WeekId, x.Status });
        builder.HasIndex(x => new { x.StudentId, x.WeekId });
        builder.HasIndex(x => new { x.Status, x.ReportedAt });
        builder.HasIndex(x => x.BulkGroupId);
        builder.HasIndex(x => new { x.RemediationStatus, x.RemediationDeadline });

        builder.Property(x => x.Quantity).HasDefaultValue(1);
        builder.Property(x => x.Status).HasDefaultValue(ViolationStatus.PENDING);
        builder.Property(x => x.IsBulk).HasDefaultValue(false);
        builder.Property(x => x.IsFlagged).HasDefaultValue(false);
        builder.Property(x => x.RemediationStatus).HasDefaultValue(RemediationStatus.NOT_REQUIRED);
        builder.Property(x => x.ReportedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.IsLocked).HasDefaultValue(false);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Week)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.WeekId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Type)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReportedBy)
            .WithMany(x => x.Reported)
            .HasForeignKey(x => x.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany(x => x.Reviewed)
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.RemediationConfirmedBy)
            .WithMany(x => x.RemediationsOk)
            .HasForeignKey(x => x.RemediationConfirmedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
