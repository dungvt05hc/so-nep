using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ConductAdjustmentConfiguration : IEntityTypeConfiguration<ConductAdjustment>
{
    public void Configure(EntityTypeBuilder<ConductAdjustment> builder)
    {
        builder.ToTable("conduct_adjustments");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.StudentId, x.TermId }).IsUnique();
        builder.HasIndex(x => x.TermId);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Term)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LevelBefore)
            .WithMany(x => x.AdjBefore)
            .HasForeignKey(x => x.LevelBeforeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LevelAfter)
            .WithMany(x => x.AdjAfter)
            .HasForeignKey(x => x.LevelAfterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DecidedBy)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
