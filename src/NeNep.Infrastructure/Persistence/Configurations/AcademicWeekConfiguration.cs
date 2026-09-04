using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class AcademicWeekConfiguration : IEntityTypeConfiguration<AcademicWeek>
{
    public void Configure(EntityTypeBuilder<AcademicWeek> builder)
    {
        builder.ToTable("academic_weeks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.YearId, x.WeekNo }).IsUnique();
        builder.HasIndex(x => new { x.StartDate, x.EndDate });

        builder.Property(x => x.IsCounted).HasDefaultValue(true);
        builder.Property(x => x.Status).HasDefaultValue(WeekStatus.OPEN);

        builder.HasOne(x => x.Year)
            .WithMany(x => x.Weeks)
            .HasForeignKey(x => x.YearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Term)
            .WithMany(x => x.Weeks)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
