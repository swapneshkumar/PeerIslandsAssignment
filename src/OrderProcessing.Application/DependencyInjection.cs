using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Orders.Services;
using OrderProcessing.Shared.Time;

namespace OrderProcessing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
