using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Options;
using OrderProcessing.Infrastructure.Caching;
using OrderProcessing.Infrastructure.Jobs;
using OrderProcessing.Infrastructure.Security;
using Serilog;

namespace OrderProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrderProcessingOptions>(configuration.GetSection(OrderProcessingOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSerilog((_, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(configuration).WriteTo.Console());

        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }

        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddScoped<IOrderProcessingJob, PendingOrderProcessorJob>();

        ConfigureAuthentication(services, configuration);
        ConfigureHangfire(services, configuration);

        return services;
    }

    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<IOrderProcessingJob>(
            "process-pending-orders",
            job => job.ProcessPendingOrdersAsync(CancellationToken.None),
            "*/5 * * * *");
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanReadOrders", policy => policy.RequireRole("Customer", "Admin", "Manager"))
            .AddPolicy("CanManageOrders", policy => policy.RequireRole("Admin", "Manager"))
            .AddPolicy("CanCancelOrders", policy => policy.RequireRole("Customer", "Admin", "Manager"));
    }

    private static void ConfigureHangfire(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();
    }
}
