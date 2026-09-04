using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>
/// The small school every test works against: one year, two terms, two classes of grade
/// 9 with a homeroom teacher each, and one student per class.
/// <para>
/// Every test seeds its own copy with a unique suffix, so the tests share one database
/// without sharing any rows.
/// </para>
/// </summary>
public sealed record TestSchool(
    string Token,
    int YearId,
    int Hk1Id,
    int Hk2Id,
    int GradeId,
    int Class91Id,
    int Class92Id,
    int Teacher91Id,
    string Teacher91Username,
    int Teacher92Id,
    string Teacher92Username,
    int AdminId,
    string AdminUsername,
    int Student91Id,
    int Student92Id)
{
    public const string Password = "MatKhau@123";

    public static async Task<TestSchool> SeedAsync(ApiFactory factory)
    {
        var token = Guid.NewGuid().ToString("N")[..8];

        await using var db = factory.CreateDbContext();

        var now = factory.Time.GetUtcNow();

        var year = new AcademicYear
        {
            Name = $"2026-2027-{token}",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 5, 31),
            IsCurrent = false,
            Terms =
            {
                new Term
                {
                    Code = "HK1",
                    Name = "Học kỳ 1",
                    StartDate = new DateOnly(2026, 9, 1),
                    EndDate = new DateOnly(2026, 12, 31),
                    Ordinal = 1,
                },
                new Term
                {
                    Code = "HK2",
                    Name = "Học kỳ 2",
                    StartDate = new DateOnly(2027, 1, 1),
                    EndDate = new DateOnly(2027, 5, 31),
                    Ordinal = 2,
                },
            },
        };

        db.AcademicYears.Add(year);

        var grade = await db.Grades.FirstOrDefaultAsync(g => g.Level == 9);

        if (grade is null)
        {
            grade = new Grade { Level = 9, Name = "Khối 9" };
            db.Grades.Add(grade);
        }

        var admin = NewUser($"admin-{token}", "Quản trị", now, factory);
        admin.Roles.Add(new UserRole { Role = Role.ADMIN });

        var teacher91 = NewUser($"gv91-{token}", "Cô Lan", now, factory);
        var teacher92 = NewUser($"gv92-{token}", "Thầy Minh", now, factory);

        db.Users.AddRange(admin, teacher91, teacher92);

        await db.SaveChangesAsync();

        var class91 = new Class
        {
            GradeId = grade.Id,
            YearId = year.Id,
            Code = $"9/1-{token}",
            Name = "Lớp 9/1",
            HomeroomTeacherId = teacher91.Id,
        };

        var class92 = new Class
        {
            GradeId = grade.Id,
            YearId = year.Id,
            Code = $"9/2-{token}",
            Name = "Lớp 9/2",
            HomeroomTeacherId = teacher92.Id,
        };

        db.Classes.AddRange(class91, class92);

        await db.SaveChangesAsync();

        teacher91.Roles.Add(new UserRole { Role = Role.GVCN, ClassId = class91.Id });
        teacher92.Roles.Add(new UserRole { Role = Role.GVCN, ClassId = class92.Id });

        var student91 = NewStudent($"HS91{token}", "Trần Thị Bích", now);
        var student92 = NewStudent($"HS92{token}", "Lê Văn Cường", now);

        db.Students.AddRange(student91, student92);

        await db.SaveChangesAsync();

        db.Enrollments.AddRange(
            new Enrollment { StudentId = student91.Id, ClassId = class91.Id, YearId = year.Id, OrderNo = 1, IsActive = true },
            new Enrollment { StudentId = student92.Id, ClassId = class92.Id, YearId = year.Id, OrderNo = 1, IsActive = true });

        await db.SaveChangesAsync();

        return new TestSchool(
            token,
            year.Id,
            year.Terms.First(t => t.Code == "HK1").Id,
            year.Terms.First(t => t.Code == "HK2").Id,
            grade.Id,
            class91.Id,
            class92.Id,
            teacher91.Id,
            teacher91.Username,
            teacher92.Id,
            teacher92.Username,
            admin.Id,
            admin.Username,
            student91.Id,
            student92.Id);
    }

    /// <summary>Adds a class-officer account, the way a homeroom teacher would.</summary>
    public static async Task<int> AddOfficerAccountAsync(
        ApiFactory factory,
        int classId,
        int studentId,
        string username,
        Role role = Role.LOP_TRUONG,
        bool mustChangePassword = false)
    {
        await using var db = factory.CreateDbContext();

        var user = new User
        {
            Username = username,
            PasswordHash = TestPasswords.Hash(Password),
            FullName = "Cán bộ lớp",
            IsActive = true,
            MustChangePassword = mustChangePassword,
            StudentId = studentId,
            CreatedAt = factory.Time.GetUtcNow(),
            Roles = { new UserRole { Role = role, ClassId = classId } },
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return user.Id;
    }

    private static User NewUser(string username, string fullName, DateTimeOffset now, ApiFactory factory) =>
        new()
        {
            Username = username,
            PasswordHash = TestPasswords.Hash(Password),
            FullName = fullName,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = now,
        };

    private static Student NewStudent(string code, string fullName, DateTimeOffset now) =>
        new()
        {
            Code = code,
            FullName = fullName,
            Gender = "Nam",
            CreatedAt = now,
        };
}
