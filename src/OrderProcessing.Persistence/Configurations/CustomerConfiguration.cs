using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Customers;

namespace OrderProcessing.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.OwnsOne(x => x.ShippingAddress, owned =>
        {
            owned.Property(x => x.Line1).HasColumnName("shipping_line1").HasMaxLength(200).IsRequired();
            owned.Property(x => x.Line2).HasColumnName("shipping_line2").HasMaxLength(200);
            owned.Property(x => x.City).HasColumnName("shipping_city").HasMaxLength(100).IsRequired();
            owned.Property(x => x.State).HasColumnName("shipping_state").HasMaxLength(100).IsRequired();
            owned.Property(x => x.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20).IsRequired();
            owned.Property(x => x.Country).HasColumnName("shipping_country").HasMaxLength(100).IsRequired();
        });
    }
}
