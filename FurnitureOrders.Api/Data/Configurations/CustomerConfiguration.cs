using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurnitureOrders.Api.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        _ = b.HasKey(x => x.Id);

        _ = b.Property(x => x.CustomerNumber).IsRequired().HasMaxLength(30);
        _ = b.HasIndex(x => x.CustomerNumber).IsUnique();

        _ = b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        _ = b.Property(x => x.CompanyId).HasMaxLength(20);
        _ = b.Property(x => x.VatId).HasMaxLength(20);
        _ = b.Property(x => x.ContactPerson).HasMaxLength(150);
        _ = b.Property(x => x.Email).HasMaxLength(200);
        _ = b.Property(x => x.Phone).HasMaxLength(50);
        _ = b.Property(x => x.Street).HasMaxLength(200);
        _ = b.Property(x => x.City).HasMaxLength(100);
        _ = b.Property(x => x.PostalCode).HasMaxLength(20);
        _ = b.Property(x => x.Country).HasMaxLength(100);
        _ = b.Property(x => x.Note).HasMaxLength(2000);
    }
}