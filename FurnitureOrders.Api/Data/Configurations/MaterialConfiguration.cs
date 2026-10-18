using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.Property(x => x.Code).IsRequired().HasMaxLength(30);
        _ = b.HasIndex(x => x.Code).IsUnique();

        _ = b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        _ = b.Property(x => x.Description).HasMaxLength(1000);
        _ = b.Property(x => x.Unit).IsRequired().HasMaxLength(20);
        _ = b.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        _ = b.Property(x => x.Supplier).HasMaxLength(200);
        _ = b.Property(x => x.StockNumber).HasMaxLength(50);
        _ = b.Property(x => x.MinOrderQuantity).HasPrecision(18, 3);
        _ = b.Property(x => x.ColorOrDecor).HasMaxLength(100);
        _ = b.Property(x => x.Note).HasMaxLength(2000);
    }
}