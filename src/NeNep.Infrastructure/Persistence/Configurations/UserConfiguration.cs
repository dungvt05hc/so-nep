using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.StudentId).IsUnique();

        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.MustChangePassword).HasDefaultValue(true);
        builder.Property(x => x.FailedAttempts).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Student)
            .WithOne(x => x.Account)
            .HasForeignKey<User>(x => x.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedBy)
            .WithMany(x => x.Created)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
