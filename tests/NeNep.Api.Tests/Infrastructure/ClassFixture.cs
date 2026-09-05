using NeNep.Domain.Enums;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>
/// One class ready to record against: three weeks of term 1, three pupils, a homeroom
/// teacher, and a class monitor account belonging to one of the pupils.
/// <para>
/// Every test builds its own, so nothing a test writes can reach another one.
/// </para>
/// </summary>
public sealed class ClassFixture
{
    private readonly ApiFactory _factory;

    private ClassFixture(
        ApiFactory factory,
        TestSchool school,
        IReadOnlyList<int> weekIds,
        int studentId,
        int otherStudentId,
        int officerStudentId,
        string officerUsername)
    {
        _factory = factory;
        School = school;
        WeekIds = weekIds;
        StudentId = studentId;
        OtherStudentId = otherStudentId;
        OfficerStudentId = officerStudentId;
        OfficerUsername = officerUsername;
    }

    public TestSchool School { get; }

    public int ClassId => School.Class91Id;

    public IReadOnlyList<int> WeekIds { get; }

    /// <summary>Week 1 — over and done with, the one <see cref="TestClassData.LastFriday"/> falls in.</summary>
    public int PreviousWeekId => WeekIds[0];

    /// <summary>Week 2 — the week the test clock is living in.</summary>
    public int CurrentWeekId => WeekIds[1];

    /// <summary>Week 3 — has not started yet.</summary>
    public int FutureWeekId => WeekIds[2];

    /// <summary>An ordinary pupil, the one <see cref="TestSchool"/> creates.</summary>
    public int StudentId { get; }

    /// <summary>A second ordinary pupil, for records the class monitor may enter.</summary>
    public int OtherStudentId { get; }

    /// <summary>The pupil the class monitor account belongs to.</summary>
    public int OfficerStudentId { get; }

    public string OfficerUsername { get; }

    public static async Task<ClassFixture> CreateAsync(ApiFactory factory)
    {
        var school = await TestSchool.SeedAsync(factory);
        var weeks = await TestClassData.AddWeeksAsync(factory, school);

        var other = await TestClassData.AddStudentAsync(
            factory,
            school,
            school.Class91Id,
            $"HSB{school.Token}",
            "Phạm Thị Hồng",
            2);

        var officerStudent = await TestClassData.AddStudentAsync(
            factory,
            school,
            school.Class91Id,
            $"HSC{school.Token}",
            "Nguyễn Văn Đức",
            3);

        await TestClassData.AddOfficerAppointmentAsync(factory, school.Class91Id, officerStudent);

        var username = $"lt91-{school.Token}";

        await TestSchool.AddOfficerAccountAsync(
            factory,
            school.Class91Id,
            officerStudent,
            username,
            Role.LOP_TRUONG);

        return new ClassFixture(
            factory,
            school,
            weeks,
            school.Student91Id,
            other,
            officerStudent,
            username);
    }

    public Task<HttpClient> SignInTeacherAsync() =>
        _factory.SignInAsync(School.Teacher91Username, TestSchool.Password);

    public Task<HttpClient> SignInOfficerAsync() =>
        _factory.SignInAsync(OfficerUsername, TestSchool.Password);

    public Task<HttpClient> SignInAdminAsync() =>
        _factory.SignInAsync(School.AdminUsername, TestSchool.Password);

    public Task<int> TypeIdAsync(string code) => TestClassData.TypeIdAsync(_factory, code);
}
