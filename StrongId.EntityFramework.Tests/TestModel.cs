using Microsoft.EntityFrameworkCore;
using StrongId.Attributes;
using StrongId.Configuration;
using StrongId.EntityFramework.Extension;

namespace StrongId.EntityFramework.Tests;

[StrongIdPrefix("prod")]
public partial class ProductId;

[StrongIdPrefix("cust")]
public partial class CustomerId;

[StrongIdPrefix("tag")]
public partial class TagId;

// Default IdType (Uuid7) + default StoreType (NativeType) → auto picks UUID converter.
[StrongIdPrefix("au")]
public partial class AutoUuidId;

// SequenceString + NativeType → auto picks string converter.
[StrongIdPrefix("as", IdType.SequenceString)]
public partial class AutoSeqId;

// Uuid7 but explicit StoreType.String → auto picks string converter (attribute overrides native).
[StrongIdPrefix("ax", IdType.Uuid7, StoreType.String)]
public partial class AutoForcedStringId;

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

public class AutoUuidEntity
{
    public required AutoUuidId Id { get; set; }
    public required string Name { get; set; }
}

public class AutoSeqEntity
{
    public required AutoSeqId Id { get; set; }
    public required string Name { get; set; }
}

public class AutoForcedStringEntity
{
    public required AutoForcedStringId Id { get; set; }
    public required string Name { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AutoUuidEntity> AutoUuids => Set<AutoUuidEntity>();
    public DbSet<AutoSeqEntity> AutoSeqs => Set<AutoSeqEntity>();
    public DbSet<AutoForcedStringEntity> AutoForcedStrings => Set<AutoForcedStringEntity>();

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

        modelBuilder.Entity<AutoUuidEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasAutoConversion();
        });

        modelBuilder.Entity<AutoSeqEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasAutoConversion();
        });

        modelBuilder.Entity<AutoForcedStringEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasAutoConversion();
        });
    }
}
