using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.Property(x => x.Code).IsRequired().HasMaxLength(30);
        _ = b.HasIndex(x => x.Code).IsUnique();

        _ = b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        _ = b.Property(x => x.Description).HasMaxLength(1000);
        _ = b.Property(x => x.ProductGroup).HasMaxLength(100);
        _ = b.Property(x => x.ProductType).HasMaxLength(100);
        _ = b.Property(x => x.Unit).IsRequired().HasMaxLength(20);
        _ = b.Property(x => x.SalesPrice).HasPrecision(18, 2);
        _ = b.Property(x => x.Width).HasPrecision(10, 1);
        _ = b.Property(x => x.Height).HasPrecision(10, 1);
        _ = b.Property(x => x.Depth).HasPrecision(10, 1);
        _ = b.Property(x => x.Weight).HasPrecision(10, 2);
        _ = b.Property(x => x.Note).HasMaxLength(2000);
    }
}