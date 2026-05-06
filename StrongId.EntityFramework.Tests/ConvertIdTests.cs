using Microsoft.EntityFrameworkCore;
using Shouldly;
using StrongId.Base;
using StrongId.EntityFramework.Converters;
using Xunit;

namespace StrongId.EntityFramework.Tests;

[Collection(nameof(SqliteCollection))]
public class ConvertToUuIdTests(SqliteFixture fixture)
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsStrongId()
    {
        var id = ProductId.Create();
        var product = new Product { Id = id, Name = "Widget" };

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var loaded = await ctx.Products.SingleAsync(p => p.Id == id);
            loaded.Id.Value.ShouldBe(id.Value);
            loaded.Name.ShouldBe("Widget");
        }
    }

    [Fact]
    public async Task IdIsStoredAsTextColumn()
    {
        var id = ProductId.Create();
        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.Add(new Product { Id = id, Name = "Storage" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT type FROM pragma_table_info('Products') WHERE name = 'Id'";
        var dataType = (string?)await cmd.ExecuteScalarAsync();
        dataType.ShouldBe("TEXT");
    }

    [Fact]
    public async Task UuidStoredMatchesStrongIdHexPart()
    {
        var id = ProductId.Create();
        var hex = id.Value.Split('_')[1];
        var expectedGuid = Guid.ParseExact(hex, "N");

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.Add(new Product { Id = id, Name = "Hex" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT \"Id\" FROM \"Products\" WHERE \"Name\" = 'Hex' LIMIT 1";
        var stored = Guid.Parse((string)(await cmd.ExecuteScalarAsync())!);
        stored.ShouldBe(expectedGuid);
    }

    [Fact]
    public async Task QueryByStrongId_Filters()
    {
        var keep = ProductId.Create();
        var skip = ProductId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.AddRange(
                new Product { Id = keep, Name = "Keep" },
                new Product { Id = skip, Name = "Skip" });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var hits = await ctx.Products.Where(p => p.Id == keep).ToListAsync();
            hits.Count.ShouldBe(1);
            hits[0].Name.ShouldBe("Keep");
        }
    }

    [Fact]
    public async Task NullableStrongId_PersistsNull()
    {
        var id = ProductId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.Add(new Product { Id = id, Name = "NullOwner", OwnerId = null });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var loaded = await ctx.Products.SingleAsync(p => p.Id == id);
            loaded.OwnerId.ShouldBeNull();
        }
    }

    [Fact]
    public async Task NullableStrongId_PersistsValue()
    {
        var id = ProductId.Create();
        var owner = CustomerId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Products.Add(new Product { Id = id, Name = "WithOwner", OwnerId = owner });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var loaded = await ctx.Products.SingleAsync(p => p.Id == id);
            loaded.OwnerId!.Value.ShouldBe(owner.Value);
        }
    }

    [Fact]
    public void ConvertId_ConvertsToAndFromGuid()
    {
        var id = ProductId.Create();
        var converter = new Converter.ConvertToUUId<ProductId>();

        var asGuid = (Guid)converter.ConvertToProvider(id)!;
        Guid.ParseExact(id.Value.Split('_')[1], "N").ShouldBe(asGuid);

        var roundTrip = (ProductId)converter.ConvertFromProvider(asGuid)!;
        roundTrip.Value.ShouldBe(id.Value);
    }
}
