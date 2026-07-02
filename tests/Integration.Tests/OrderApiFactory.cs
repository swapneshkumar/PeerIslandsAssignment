using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Integration.Tests;

public sealed class OrderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtSigningKey = "integration-test-signing-key-change-me-32";

    static OrderApiFactory()
    {
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("order_processing_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string JwtIssuer => "OrderProcessing";
    public string JwtAudience => "OrderProcessing.Client";
    public string SigningKey => JwtSigningKey;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", string.Empty);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtSigningKey);
        Environment.SetEnvironmentVariable("OrderProcessing__PendingOrderThresholdMinutes", "5");
        Environment.SetEnvironmentVariable("OrderProcessing__PendingOrderBatchSize", "100");
        Environment.SetEnvironmentVariable("OrderProcessing__OrderCacheMinutes", "5");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
        Environment.SetEnvironmentVariable("OrderProcessing__PendingOrderThresholdMinutes", null);
        Environment.SetEnvironmentVariable("OrderProcessing__PendingOrderBatchSize", null);
        Environment.SetEnvironmentVariable("OrderProcessing__OrderCacheMinutes", null);
        await postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = string.Empty,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["OrderProcessing:PendingOrderThresholdMinutes"] = "5",
                ["OrderProcessing:PendingOrderBatchSize"] = "100",
                ["OrderProcessing:OrderCacheMinutes"] = "5"
            });
        });
    }
}
