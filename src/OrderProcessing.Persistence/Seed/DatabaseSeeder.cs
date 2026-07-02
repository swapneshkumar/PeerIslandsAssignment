using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OrderProcessing.Domain.Customers;
using OrderProcessing.Domain.Products;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderProcessingDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureApplicationTablesAsync(dbContext, cancellationToken);

        if (!await dbContext.Customers.AnyAsync(cancellationToken))
        {
            dbContext.Customers.Add(Customer.Create(
                "Demo Customer",
                "customer@example.com",
                Address.Create("100 Market Street", null, "San Francisco", "CA", "94105", "US")));
        }

        if (!await dbContext.Products.AnyAsync(cancellationToken))
        {
            dbContext.Products.AddRange(
                Product.Create("SKU-LAPTOP-001", "Enterprise Laptop", Money.Create(1299.99m, "USD")),
                Product.Create("SKU-DOCK-001", "USB-C Dock", Money.Create(199.99m, "USD")));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureApplicationTablesAsync(OrderProcessingDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await TableExistsAsync(dbContext, "customers", cancellationToken))
        {
            return;
        }

        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
        await databaseCreator.CreateTablesAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(OrderProcessingDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from information_schema.tables
                where table_schema = 'public'
                  and table_name = @tableName
            );
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }
}
