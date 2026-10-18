using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class ProductOperationConfiguration : IEntityTypeConfiguration<ProductOperation>
{
    public void Configure(EntityTypeBuilder<ProductOperation> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = b.HasOne(x => x.Operation).WithMany().HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = b.HasIndex(x => new { x.ProductId, x.Sequence });

        _ = b.Property(x => x.Workplace).HasMaxLength(100);
        _ = b.Property(x => x.RunMinutesPerUnit).HasPrecision(10, 2);
        _ = b.Property(x => x.WorkInstructions).HasMaxLength(4000);
        _ = b.Property(x => x.Note).HasMaxLength(1000);
    }
}