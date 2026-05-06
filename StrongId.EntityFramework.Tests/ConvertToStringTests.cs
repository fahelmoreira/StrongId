using Microsoft.EntityFrameworkCore;
using Shouldly;
using StrongId.EntityFramework.Converters;
using Xunit;

namespace StrongId.EntityFramework.Tests;

[Collection(nameof(SqliteCollection))]
public class ConvertToStringTests(SqliteFixture fixture)
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsTagId()
    {
        var id = TagId.Create();
        var tag = new Tag { Id = id, Label = "important" };

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var loaded = await ctx.Tags.SingleAsync(t => t.Id == id);
            loaded.Id.Value.ShouldBe(id.Value);
            loaded.Label.ShouldBe("important");
        }
    }

    [Fact]
    public async Task IdIsStoredAsTextColumn()
    {
        var id = TagId.Create();
        await using (var ctx = fixture.CreateContext())
        {
            ctx.Tags.Add(new Tag { Id = id, Label = "Storage" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT type FROM pragma_table_info('Tags') WHERE name = 'Id'";
        var dataType = (string?)await cmd.ExecuteScalarAsync();
        dataType.ShouldBe("TEXT");
    }

    [Fact]
    public async Task StoredValue_IsFullStrongIdStringWithPrefix()
    {
        var id = TagId.Create();
        await using (var ctx = fixture.CreateContext())
        {
            ctx.Tags.Add(new Tag { Id = id, Label = "Raw" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT \"Id\" FROM \"Tags\" WHERE \"Label\" = 'Raw' LIMIT 1";
        var stored = (string?)await cmd.ExecuteScalarAsync();
        stored.ShouldBe(id.Value);
        stored.ShouldStartWith("tag_");
    }

    [Fact]
    public async Task QueryByStrongId_Filters()
    {
        var keep = TagId.Create();
        var skip = TagId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Tags.AddRange(
                new Tag { Id = keep, Label = "Keep" },
                new Tag { Id = skip, Label = "Skip" });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = fixture.CreateContext())
        {
            var hits = await ctx.Tags.Where(t => t.Id == keep).ToListAsync();
            hits.Count.ShouldBe(1);
            hits[0].Label.ShouldBe("Keep");
        }
    }

    [Fact]
    public void ConvertToString_ConvertsToAndFromString()
    {
        var id = TagId.Create();
        var converter = new Converter.ConvertToString<TagId>();

        var asString = (string)converter.ConvertToProvider(id)!;
        asString.ShouldBe(id.Value);

        var roundTrip = (TagId)converter.ConvertFromProvider(asString)!;
        roundTrip.Value.ShouldBe(id.Value);
    }

    [Fact]
    public void ConvertToString_ThrowsOnInvalidProviderValue()
    {
        var converter = new Converter.ConvertToString<TagId>();
        Should.Throw<InvalidCastException>(() => converter.ConvertFromProvider("user_019df8fe572c79e186922e1a64fd2bbf"));
    }
}
