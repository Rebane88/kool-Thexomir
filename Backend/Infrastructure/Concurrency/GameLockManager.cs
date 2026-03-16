using System.Collections.Concurrent;
using Application.Contracts;

namespace Infrastructure.Concurrency;

public class GameLockManager : IGameLockManager
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(Guid gameId, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new LockRelease(semaphore);
    }

    private sealed class LockRelease(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
