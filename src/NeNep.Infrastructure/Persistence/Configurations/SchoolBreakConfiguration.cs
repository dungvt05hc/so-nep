using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class SchoolBreakConfiguration : IEntityTypeConfiguration<SchoolBreak>
{
    public void Configure(EntityTypeBuilder<SchoolBreak> builder)
    {
        builder.ToTable("school_breaks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.YearId, x.StartDate });

        builder.Property(x => x.IsConfirmed).HasDefaultValue(false);
        builder.Property(x => x.ShiftedWeeks).HasDefaultValue(0);

        builder.HasOne(x => x.Year)
            .WithMany(x => x.Breaks)
            .HasForeignKey(x => x.YearId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
