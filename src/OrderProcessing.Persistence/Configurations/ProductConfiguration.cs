using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Products;

namespace OrderProcessing.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();

        builder.OwnsOne(x => x.Price, owned =>
        {
            owned.Property(x => x.Amount).HasColumnName("price").HasPrecision(18, 2).IsRequired();
            owned.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });
    }
}
