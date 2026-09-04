using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

/// <summary>
/// The audit trail is APPEND-ONLY: no updates, no deletes, no soft deletes.
/// That rule is enforced by <c>AuditSaveChangesInterceptor</c> during SaveChanges.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Entity, x.EntityId });
        builder.HasIndex(x => new { x.ActorId, x.CreatedAt });
        builder.HasIndex(x => new { x.ClassId, x.CreatedAt });

        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Actor)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Class)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
