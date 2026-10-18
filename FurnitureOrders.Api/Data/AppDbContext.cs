using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasSequence<int>("CustomerNumberSeq");
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
