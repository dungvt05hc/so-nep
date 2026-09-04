using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("classes");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.YearId, x.Code }).IsUnique();
        builder.HasIndex(x => x.HomeroomTeacherId);

        builder.HasOne(x => x.Grade)
            .WithMany(x => x.Classes)
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Year)
            .WithMany(x => x.Classes)
            .HasForeignKey(x => x.YearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HomeroomTeacher)
            .WithMany(x => x.HomeroomOf)
            .HasForeignKey(x => x.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
