using NeNep.Infrastructure.Security;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>Hashes seed passwords with the same service the application signs in with.</summary>
public static class TestPasswords
{
    private static readonly IPasswordService Service = new PasswordService();

    public static string Hash(string password) => Service.Hash(password);
}
