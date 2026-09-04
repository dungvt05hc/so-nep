using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

/// <summary>
/// NOTE ON PRINCIPLE 1 (no regulation numbers hardcoded in code).
/// The <c>HasDefaultValue</c> values below are a one-for-one copy of the
/// <c>@default(...)</c> declarations in <c>docs/schema.prisma</c>, i.e. the SEED values
/// of the single configuration row.
/// This is the ONLY place in the whole codebase where these numbers may appear.
/// Any runtime calculation must read them from the <c>school_settings</c> row, never
/// from here.
/// </summary>
public class SchoolSettingConfiguration : IEntityTypeConfiguration<SchoolSetting>
{
    public void Configure(EntityTypeBuilder<SchoolSetting> builder)
    {
        builder.ToTable("school_settings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValue(1).ValueGeneratedNever();

        builder.Property(x => x.SchoolCode).HasDefaultValue("");

        builder.Property(x => x.BaseScore).HasDefaultValue(100);
        builder.Property(x => x.AutoApplyLevelRules).HasDefaultValue(false);
        builder.Property(x => x.AlertLeadCount).HasDefaultValue(2);
        builder.Property(x => x.AllowClassPointOverride).HasDefaultValue(false);
        builder.Property(x => x.StackAutoBonus).HasDefaultValue(true);

        builder.Property(x => x.LockDayOfWeek).HasDefaultValue(0);
        builder.Property(x => x.LockHour).HasDefaultValue(20);
        builder.Property(x => x.GraceHours).HasDefaultValue(16);
        builder.Property(x => x.ExpirePendingOnLock).HasDefaultValue(true);

        builder.Property(x => x.ParentCodeLength).HasDefaultValue(6);
        builder.Property(x => x.ParentMaxFailedTries).HasDefaultValue(5);
        builder.Property(x => x.ParentLockMinutes).HasDefaultValue(30);
        builder.Property(x => x.ParentShowRank).HasDefaultValue(true);

        // @updatedAt — stamped by TimestampInterceptor on SaveChanges.
        builder.Property(x => x.UpdatedAt).HasAnnotation(NeNepAnnotations.UpdatedAt, true);
    }
}
