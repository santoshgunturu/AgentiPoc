namespace AgenticPA.Agent.Skills;

/// <summary>
/// Thread-safe tracker of in-flight skill turns. Used by the nightly rubric-refresh
/// service to avoid reloading mid-conversation.
/// </summary>
public class InFlightCounter
{
    private int _count;
    private long _lastActivityTicks = DateTime.UtcNow.Ticks;

    public int CurrentCount => Volatile.Read(ref _count);

    public DateTime LastActivityUtc => new(Interlocked.Read(ref _lastActivityTicks), DateTimeKind.Utc);

    public TimeSpan IdleFor => DateTime.UtcNow - LastActivityUtc;

    public IDisposable BeginTurn()
    {
        Interlocked.Increment(ref _count);
        TouchActivity();
        return new Lease(this);
    }

    public void TouchActivity()
    {
        Interlocked.Exchange(ref _lastActivityTicks, DateTime.UtcNow.Ticks);
    }

    private void EndTurn()
    {
        Interlocked.Decrement(ref _count);
        TouchActivity();
    }

    private sealed class Lease : IDisposable
    {
        private readonly InFlightCounter _owner;
        private int _disposed;
        public Lease(InFlightCounter owner) => _owner = owner;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _owner.EndTurn();
        }
    }
}
