namespace EventHub.Api.Tests.Fakes;

/// <summary>
/// Real clock plus an adjustable offset. With Offset = 0 it behaves like TimeProvider.System,
/// so all other tests are unaffected. A test moves time forward via <see cref="Advance"/>
/// and the returned scope resets the offset on dispose.
/// </summary>
public sealed class ShiftableTimeProvider : TimeProvider
{
    private long _offsetTicks;

    public TimeSpan Offset => TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + Offset;

    public IDisposable Advance(TimeSpan by)
    {
        Interlocked.Add(ref _offsetTicks, by.Ticks);
        return new Reset(this);
    }

    private sealed class Reset(ShiftableTimeProvider owner) : IDisposable
    {
        public void Dispose() => Interlocked.Exchange(ref owner._offsetTicks, 0);
    }
}
