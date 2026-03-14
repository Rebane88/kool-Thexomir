namespace Base.Contracts;

public interface IBaseRepository<TEntity> where TEntity : class, IBaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
