using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace StrongId.EntityFramework.Tests;

[Collection(nameof(SqliteCollection))]
public class AutoConversionTests(SqliteFixture fixture)
{
    [Fact]
    public async Task NativeStore_WithUuidIdType_UsesUuidConverter()
    {
        var id = AutoUuidId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.AutoUuids.Add(new AutoUuidEntity { Id = id, Name = "uuid-native" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT \"Id\" FROM \"AutoUuids\" WHERE \"Name\" = 'uuid-native' LIMIT 1";
        var stored = (string)(await cmd.ExecuteScalarAsync())!;

        // UUID converter stores the bare GUID (no prefix, hyphenated SQLite default form).
        Guid.TryParse(stored, out _).ShouldBeTrue();
        stored.ShouldNotStartWith("au_");

        await using var ctx2 = fixture.CreateContext();
        var loaded = await ctx2.AutoUuids.SingleAsync(e => e.Id == id);
        loaded.Id.Value.ShouldBe(id.Value);
    }

    [Fact]
    public async Task NativeStore_WithSequenceStringIdType_UsesStringConverter()
    {
        var id = AutoSeqId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.AutoSeqs.Add(new AutoSeqEntity { Id = id, Name = "seq-native" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT \"Id\" FROM \"AutoSeqs\" WHERE \"Name\" = 'seq-native' LIMIT 1";
        var stored = (string)(await cmd.ExecuteScalarAsync())!;

        // String converter persists the full prefixed value.
        stored.ShouldBe(id.Value);
        stored.ShouldStartWith("as_");

        await using var ctx2 = fixture.CreateContext();
        var loaded = await ctx2.AutoSeqs.SingleAsync(e => e.Id == id);
        loaded.Id.Value.ShouldBe(id.Value);
    }

    [Fact]
    public async Task StringStore_OverridesNativeUuid_UsesStringConverter()
    {
        var id = AutoForcedStringId.Create();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.AutoForcedStrings.Add(new AutoForcedStringEntity { Id = id, Name = "uuid-forced-string" });
            await ctx.SaveChangesAsync();
        }

        await using var cmd = fixture.RawConnection.CreateCommand();
        cmd.CommandText = "SELECT \"Id\" FROM \"AutoForcedStrings\" WHERE \"Name\" = 'uuid-forced-string' LIMIT 1";
        var stored = (string)(await cmd.ExecuteScalarAsync())!;

        // Even though IdType is Uuid7, StoreType.String forces the prefixed string format.
        stored.ShouldBe(id.Value);
        stored.ShouldStartWith("ax_");

        await using var ctx2 = fixture.CreateContext();
        var loaded = await ctx2.AutoForcedStrings.SingleAsync(e => e.Id == id);
        loaded.Id.Value.ShouldBe(id.Value);
    }
}
