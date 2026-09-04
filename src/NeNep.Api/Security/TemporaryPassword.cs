using System.Security.Cryptography;

namespace NeNep.Api.Security;

/// <summary>
/// Generates the one-off password printed and handed to a class officer or a teacher.
/// <para>
/// The alphabet leaves out characters that look alike in print (0/O, 1/l/I), because
/// the password is copied off paper by a twelve-year-old.
/// </para>
/// </summary>
public static class TemporaryPassword
{
    private const string Alphabet = "abcdefghijkmnpqrstuvwxyzACDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 6);

        return RandomNumberGenerator.GetString(Alphabet, length);
    }
}
