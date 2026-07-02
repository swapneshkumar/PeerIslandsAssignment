using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Orders;

namespace OrderProcessing.Persistence.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
    }
}
