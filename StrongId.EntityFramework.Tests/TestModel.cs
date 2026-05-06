using Microsoft.EntityFrameworkCore;
using StrongId.Attributes;
using StrongId.Base;
using StrongId.EntityFramework.Extension;

namespace StrongId.EntityFramework.Tests;

[StrongIdPrefix("prod")]
public partial class ProductId : StrongIdBase<ProductId>;

[StrongIdPrefix("cust")]
public partial class CustomerId : StrongIdBase<CustomerId>;

[StrongIdPrefix("tag")]
public partial class TagId : StrongIdBase<TagId>;

public class Product
{
    public required ProductId Id { get; set; }
    public required string Name { get; set; }
    public CustomerId? OwnerId { get; set; }
}

public class Tag
{
    public required TagId Id { get; set; }
    public required string Label { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasUuIdConversion();
            b.Property(p => p.OwnerId!).HasUuIdConversion();
        });

        modelBuilder.Entity<Tag>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasStringConversion();
        });
    }
}
