using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Customers;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Domain.Products;

namespace OrderProcessing.Persistence;

public sealed class OrderProcessingDbContext(DbContextOptions<OrderProcessingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderProcessingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
