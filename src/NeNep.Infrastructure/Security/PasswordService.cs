using Microsoft.AspNetCore.Identity;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Security;

/// <summary>
/// Password hashing. Only <see cref="PasswordHasher{TUser}"/> is borrowed from ASP.NET
/// Core Identity — the full Identity stack is far too heavy for 255 accounts that the
/// homeroom teachers hand out by hand.
/// </summary>
public interface IPasswordService
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password. <paramref name="needsRehash"/> is true when the stored hash
    /// uses an older format and should be replaced on the next successful sign-in.
    /// </summary>
    bool Verify(string hash, string password, out bool needsRehash);
}

public sealed class PasswordService : IPasswordService
{
    // The hasher never reads the user object, so one throwaway instance is enough.
    private static readonly User Dummy = new();

    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public bool Verify(string hash, string password, out bool needsRehash)
    {
        var result = _hasher.VerifyHashedPassword(Dummy, hash, password);

        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;

        return result != PasswordVerificationResult.Failed;
    }
}
