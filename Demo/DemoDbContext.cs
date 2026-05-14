using Microsoft.EntityFrameworkCore;
using StrongId.EntityFramework.Extension;

namespace Demo;

public class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ShoppingList> Lists => Set<ShoppingList>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // HasAutoConversion picks UUID or string storage based on each id's
        // [StrongIdPrefix] attribute — no per-property branching needed here.
        b.Entity<Product>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasAutoConversion(); });
        b.Entity<Customer>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasAutoConversion(); });
        b.Entity<Order>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasAutoConversion(); });
        b.Entity<ShoppingList>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasAutoConversion(); });
    }
}
