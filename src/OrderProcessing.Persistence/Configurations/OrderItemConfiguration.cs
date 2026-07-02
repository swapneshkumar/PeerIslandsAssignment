using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Orders;

namespace OrderProcessing.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();

        builder.OwnsOne(x => x.UnitPrice, owned =>
        {
            owned.Property(x => x.Amount).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            owned.Property(x => x.Currency).HasColumnName("unit_currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(x => x.LineTotal, owned =>
        {
            owned.Property(x => x.Amount).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();
            owned.Property(x => x.Currency).HasColumnName("line_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(x => x.ProductId);
    }
}
