using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ClassOfficerConfiguration : IEntityTypeConfiguration<ClassOfficer>
{
    public void Configure(EntityTypeBuilder<ClassOfficer> builder)
    {
        builder.ToTable("class_officers");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ClassId, x.Role });

        builder.HasOne(x => x.Class)
            .WithMany(x => x.Officers)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Student)
            .WithMany(x => x.OfficerRoles)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
