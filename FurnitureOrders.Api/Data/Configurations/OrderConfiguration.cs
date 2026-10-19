using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(30);
        _ = b.HasIndex(x => x.OrderNumber).IsUnique();

        _ = b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        _ = b.HasIndex(x => x.CustomerId);
        _ = b.HasIndex(x => x.CreatedDate);

        _ = b.Property(x => x.ContactPerson).HasMaxLength(150);
        _ = b.Property(x => x.Email).HasMaxLength(200);
        _ = b.Property(x => x.Phone).HasMaxLength(50);
        _ = b.Property(x => x.DeliveryAddress).HasMaxLength(500);
        _ = b.Property(x => x.ShippingMethod).HasMaxLength(100);
        _ = b.Property(x => x.TotalPrice).HasPrecision(18, 2);
        _ = b.Property(x => x.Note).HasMaxLength(2000);

        _ = b.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}