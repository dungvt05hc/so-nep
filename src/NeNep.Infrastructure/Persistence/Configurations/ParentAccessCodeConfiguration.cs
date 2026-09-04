using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class ParentAccessCodeConfiguration : IEntityTypeConfiguration<ParentAccessCode>
{
    public void Configure(EntityTypeBuilder<ParentAccessCode> builder)
    {
        builder.ToTable("parent_access_codes");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.RevokedAt });

        builder.Property(x => x.IssuedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.FailedAttempts).HasDefaultValue(0);
        builder.Property(x => x.ViewCount).HasDefaultValue(0);

        builder.HasOne(x => x.Student)
            .WithMany(x => x.AccessCodes)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IssuedBy)
            .WithMany(x => x.IssuedCodes)
            .HasForeignKey(x => x.IssuedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
