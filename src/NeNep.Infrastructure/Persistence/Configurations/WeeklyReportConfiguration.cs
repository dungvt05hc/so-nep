using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class WeeklyReportConfiguration : IEntityTypeConfiguration<WeeklyReport>
{
    public void Configure(EntityTypeBuilder<WeeklyReport> builder)
    {
        builder.ToTable("weekly_reports");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ClassId, x.WeekId }).IsUnique();

        builder.Property(x => x.Status).HasDefaultValue(WeekStatus.PENDING_REVIEW);
        builder.Property(x => x.UnlockCount).HasDefaultValue(0);
        builder.Property(x => x.Snapshot).HasColumnType("jsonb");

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Week)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.WeekId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
