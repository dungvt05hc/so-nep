using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>
/// The extra rows the record-keeping tests need on top of <see cref="TestSchool"/>:
/// academic weeks to file records into, more pupils than the single one, and class
/// officer appointments.
/// <para>
/// They are NOT part of <see cref="TestSchool"/> on purpose. The calendar tests generate
/// the weeks of the year through the API and would collide with weeks seeded behind
/// their back.
/// </para>
/// </summary>
public static class TestClassData
{
    /// <summary>
    /// The Monday the test weeks start from.
    /// <para>
    /// The test clock stands at 08:00 Vietnam time on Monday 7 September 2026, so week 1
    /// is already over, week 2 is the one being lived, and week 3 is still to come. That
    /// is what a class actually looks like mid-term.
    /// </para>
    /// </summary>
    public static readonly DateOnly FirstMonday = new(2026, 8, 31);

    /// <summary>The business date the test clock stands on: the Monday of week 2.</summary>
    public static readonly DateOnly Today = new(2026, 9, 7);

    /// <summary>A weekday inside the week that has already finished.</summary>
    public static readonly DateOnly LastFriday = new(2026, 9, 4);

    /// <summary>Lays out consecutive weeks of term 1, and returns their ids in order.</summary>
    public static async Task<IReadOnlyList<int>> AddWeeksAsync(
        ApiFactory factory,
        TestSchool school,
        int count = 3)
    {
        await using var db = factory.CreateDbContext();

        var weeks = new List<AcademicWeek>();

        for (var i = 0; i < count; i++)
        {
            var start = FirstMonday.AddDays(i * 7);

            weeks.Add(new AcademicWeek
            {
                YearId = school.YearId,
                TermId = school.Hk1Id,
                WeekNo = i + 1,
                StartDate = start,
                EndDate = start.AddDays(6),
                OriginalStartDate = start,
                IsCounted = true,
                Status = WeekStatus.OPEN,
            });
        }

        db.AcademicWeeks.AddRange(weeks);

        await db.SaveChangesAsync();

        return weeks.Select(w => w.Id).ToList();
    }

    /// <summary>Closes a week, the way the Sunday evening lock will in phase 3.</summary>
    public static async Task LockWeekAsync(ApiFactory factory, int weekId)
    {
        await using var db = factory.CreateDbContext();

        var week = await db.AcademicWeeks.FindAsync(weekId)
            ?? throw new InvalidOperationException($"No academic week with id {weekId}.");

        week.Status = WeekStatus.LOCKED;
        week.LockedAt = factory.Time.GetUtcNow();

        await db.SaveChangesAsync();
    }

    /// <summary>Adds one more pupil to a class register.</summary>
    public static async Task<int> AddStudentAsync(
        ApiFactory factory,
        TestSchool school,
        int classId,
        string code,
        string fullName,
        int orderNo)
    {
        await using var db = factory.CreateDbContext();

        var student = new Student
        {
            Code = code,
            FullName = fullName,
            Gender = "Nam",
            CreatedAt = factory.Time.GetUtcNow(),
            Enrollments =
            {
                new Enrollment
                {
                    ClassId = classId,
                    YearId = school.YearId,
                    OrderNo = orderNo,
                    IsActive = true,
                },
            },
        };

        db.Students.Add(student);

        await db.SaveChangesAsync();

        return student.Id;
    }

    /// <summary>Appoints a pupil as a class officer for the rest of the year.</summary>
    public static async Task AddOfficerAppointmentAsync(
        ApiFactory factory,
        int classId,
        int studentId,
        Role role = Role.LOP_TRUONG)
    {
        await using var db = factory.CreateDbContext();

        db.ClassOfficers.Add(new ClassOfficer
        {
            ClassId = classId,
            StudentId = studentId,
            Role = role,
            ValidFrom = FirstMonday,
        });

        await db.SaveChangesAsync();
    }

    /// <summary>The id of a catalog code, e.g. "N01".</summary>
    public static async Task<int> TypeIdAsync(ApiFactory factory, string code)
    {
        await using var db = factory.CreateDbContext();

        var type = db.ViolationTypes.FirstOrDefault(t => t.Code == code && t.ClassId == null)
            ?? throw new InvalidOperationException($"The catalog has no code {code}.");

        return type.Id;
    }
}
