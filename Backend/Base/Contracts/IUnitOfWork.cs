namespace Base.Contracts;

public interface IUnitOfWork : IDisposable
{
    Task<int> CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
