using OrderProcessing.Application.Abstractions;

namespace OrderProcessing.Persistence;

public sealed class UnitOfWork(OrderProcessingDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
