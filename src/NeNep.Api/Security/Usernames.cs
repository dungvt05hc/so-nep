namespace NeNep.Api.Security;

/// <summary>
/// One spelling rule for usernames, applied both when an account is created and when
/// somebody signs in, so "GV.Lan" and "gv.lan" can never become two accounts.
/// </summary>
public static class Usernames
{
    public static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
