using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Persistence.Repositories;

namespace OrderProcessing.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderProcessingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Postgres"));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
