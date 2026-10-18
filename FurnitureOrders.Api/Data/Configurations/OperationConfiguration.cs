using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
    public void Configure(EntityTypeBuilder<Operation> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.Property(x => x.Code).IsRequired().HasMaxLength(30);
        _ = b.HasIndex(x => x.Code).IsUnique();

        _ = b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        _ = b.Property(x => x.Description).HasMaxLength(1000);
        _ = b.Property(x => x.Workplace).HasMaxLength(100);
        _ = b.Property(x => x.OperationType).HasMaxLength(100);
        _ = b.Property(x => x.HourlyRate).HasPrecision(18, 2);
        _ = b.Property(x => x.RequiredQualification).HasMaxLength(100);
        _ = b.Property(x => x.Note).HasMaxLength(2000);
    }
}