using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Abstractions;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence;

/// <summary>
/// DbContext for the conduct tracking system.
/// All entity configuration lives in dedicated <c>IEntityTypeConfiguration</c> classes
/// under <c>Persistence/Configurations</c>.
/// </summary>
public class NeNepDbContext : DbContext
{
    public NeNepDbContext(DbContextOptions<NeNepDbContext> options)
        : base(options)
    {
    }

    public DbSet<SchoolSetting> SchoolSettings => Set<SchoolSetting>();
    public DbSet<ClassificationLevel> ClassificationLevels => Set<ClassificationLevel>();

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<AcademicWeek> AcademicWeeks => Set<AcademicWeek>();
    public DbSet<SchoolBreak> SchoolBreaks => Set<SchoolBreak>();

    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassOfficer> ClassOfficers => Set<ClassOfficer>();

    public DbSet<ViolationCategory> ViolationCategories => Set<ViolationCategory>();
    public DbSet<ViolationType> ViolationTypes => Set<ViolationType>();
    public DbSet<ConductRule> ConductRules => Set<ConductRule>();
    public DbSet<ViolationRecord> ViolationRecords => Set<ViolationRecord>();

    public DbSet<ConductScore> ConductScores => Set<ConductScore>();
    public DbSet<ConductAlert> ConductAlerts => Set<ConductAlert>();
    public DbSet<ConductAdjustment> ConductAdjustments => Set<ConductAdjustment>();
    public DbSet<ClassViolationOverride> ClassViolationOverrides => Set<ClassViolationOverride>();
    public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();
    public DbSet<ParentAccessCode> ParentAccessCodes => Set<ParentAccessCode>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        RegisterPostgresEnums(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NeNepDbContext).Assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Registers the 11 PostgreSQL enum types. This list must match the <c>MapEnum</c>
    /// calls one for one; a missing entry means Npgsql cannot read or write that column.
    /// </summary>
    private static void RegisterPostgresEnums(ModelBuilder modelBuilder)
    {
        var translator = NeNepEnumNameTranslator.Instance;

        modelBuilder.HasPostgresEnum<Role>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<CategoryKind>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ViolationStatus>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RemediationStatus>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<WeekStatus>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RuleType>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<CompareOp>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RuleEffect>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AlertStatus>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<PeriodScope>(nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AuditAction>(nameTranslator: translator);
    }

    /// <summary>
    /// Soft delete: every entity implementing <see cref="ISoftDeletable"/> is filtered by
    /// <c>deleted_at IS NULL</c> in every query. Seeing deleted rows requires a deliberate
    /// call to <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
