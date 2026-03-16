namespace Application.Contracts;

public interface IGameLockManager
{
    Task<IDisposable> AcquireAsync(Guid gameId, CancellationToken ct = default);
}
