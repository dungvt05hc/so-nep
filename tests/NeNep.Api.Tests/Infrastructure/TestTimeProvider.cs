namespace NeNep.Api.Tests.Infrastructure;

/// <summary>
/// A clock the tests move by hand. Nothing in the system calls <c>DateTime.Now</c>, so
/// setting this provider is enough to make a token expire or a week end on demand.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public void Set(DateTimeOffset at) => _utcNow = at;
}
