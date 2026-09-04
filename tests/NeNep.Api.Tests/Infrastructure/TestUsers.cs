using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests.Infrastructure;

public static class TestUsers
{
    /// <summary>Adds an account with a school-wide role: ADMIN or BGH.</summary>
    public static async Task<int> AddSchoolWideAsync(ApiFactory factory, string username, Role role)
    {
        await using var db = factory.CreateDbContext();

        var user = new User
        {
            Username = username,
            PasswordHash = TestPasswords.Hash(TestSchool.Password),
            FullName = role == Role.BGH ? "Ban giám hiệu" : "Quản trị",
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = factory.Time.GetUtcNow(),
            Roles = { new UserRole { Role = role } },
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return user.Id;
    }
}
