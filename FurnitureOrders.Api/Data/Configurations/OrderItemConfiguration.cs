using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        _ = b.HasIndex(x => new { x.OrderId, x.Sequence });

        _ = b.Property(x => x.Description).HasMaxLength(500);
        _ = b.Property(x => x.Quantity).HasPrecision(18, 3);
        _ = b.Property(x => x.Unit).IsRequired().HasMaxLength(20);
        _ = b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        _ = b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        _ = b.Property(x => x.TotalPrice).HasPrecision(18, 2);
        _ = b.Property(x => x.Note).HasMaxLength(1000);
    }
}