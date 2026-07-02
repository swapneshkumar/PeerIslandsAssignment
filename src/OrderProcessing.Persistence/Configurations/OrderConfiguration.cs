using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.RowVersion)
            .IsConcurrencyToken(false)
            .HasDefaultValueSql("decode(md5(random()::text || clock_timestamp()::text), 'hex')");

        builder.OwnsOne(x => x.OrderNumber, owned =>
        {
            owned.Property(x => x.Value).HasColumnName("order_number").HasMaxLength(32).IsRequired();
            owned.HasIndex(x => x.Value).IsUnique();
        });

        builder.OwnsOne(x => x.TotalAmount, owned =>
        {
            owned.Property(x => x.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
            owned.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(x => x.ShippingAddress, owned =>
        {
            owned.Property(x => x.Line1).HasColumnName("shipping_line1").HasMaxLength(200).IsRequired();
            owned.Property(x => x.Line2).HasColumnName("shipping_line2").HasMaxLength(200);
            owned.Property(x => x.City).HasColumnName("shipping_city").HasMaxLength(100).IsRequired();
            owned.Property(x => x.State).HasColumnName("shipping_state").HasMaxLength(100).IsRequired();
            owned.Property(x => x.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20).IsRequired();
            owned.Property(x => x.Country).HasColumnName("shipping_country").HasMaxLength(100).IsRequired();
        });

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.CustomerId);
    }
}
