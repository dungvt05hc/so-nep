using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ConductScoreConfiguration : IEntityTypeConfiguration<ConductScore>
{
    public void Configure(EntityTypeBuilder<ConductScore> builder)
    {
        builder.ToTable("conduct_scores");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.StudentId, x.Scope, x.PeriodKey }).IsUnique();
        builder.HasIndex(x => new { x.ClassId, x.Scope, x.PeriodKey });

        builder.Property(x => x.BaseScore).HasPrecision(8, 2);
        builder.Property(x => x.BonusTotal).HasPrecision(8, 2).HasDefaultValue(0m);
        builder.Property(x => x.PenaltyTotal).HasPrecision(8, 2).HasDefaultValue(0m);
        builder.Property(x => x.RawScore).HasPrecision(8, 2);
        builder.Property(x => x.FinalScore).HasPrecision(8, 2);

        builder.Property(x => x.SuggestedDowngrades).HasDefaultValue(0);
        builder.Property(x => x.SuggestedUpgrades).HasDefaultValue(0);
        builder.Property(x => x.ComputedAt).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Year)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.YearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Week)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.WeekId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Term)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.LevelByScore)
            .WithMany(x => x.ScoresByRule)
            .HasForeignKey(x => x.LevelByScoreId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FinalLevel)
            .WithMany(x => x.ScoresFinal)
            .HasForeignKey(x => x.FinalLevelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
