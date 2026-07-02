using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Domain.Common;

namespace OrderProcessing.Persistence.Repositories;

public class Repository<T>(OrderProcessingDbContext dbContext) : IRepository<T> where T : Entity
{
    protected readonly OrderProcessingDbContext DbContext = dbContext;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyCollection<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Set<T>().AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbContext.Set<T>().Update(entity);
}
