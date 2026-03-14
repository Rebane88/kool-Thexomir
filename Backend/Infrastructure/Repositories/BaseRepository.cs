using Base;
using Base.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BaseRepository<TEntity>(AppDbContext context) : IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    protected readonly AppDbContext Context = context;

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Context.Set<TEntity>().ToListAsync();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        var entry = await Context.Set<TEntity>().AddAsync(entity);
        return entry.Entity;
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Entity with id {id} was not found.");
        Context.Set<TEntity>().Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await Context.Set<TEntity>().AnyAsync(e => e.Id == id);
    }
}
