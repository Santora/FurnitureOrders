using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class ProductMaterialConfiguration : IEntityTypeConfiguration<ProductMaterial>
{
    public void Configure(EntityTypeBuilder<ProductMaterial> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = b.HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = b.HasIndex(x => new { x.ProductId, x.Sequence });

        _ = b.Property(x => x.Quantity).HasPrecision(18, 3);
        _ = b.Property(x => x.Unit).IsRequired().HasMaxLength(20);
        _ = b.Property(x => x.WastePercent).HasPrecision(5, 2);
        _ = b.Property(x => x.Note).HasMaxLength(1000);
    }
}