using Shouldly;
using Xunit;

namespace StrongId.Tests;

public class SequenceStringTests
{
    [Fact]
    public void Create_GeneratesPrefixedIdWithEighteenCharSuffix()
    {
        var id = CartId.Create();

        id.Value.ShouldStartWith("cart_");
        id.Value.Split('_')[1].Length.ShouldBe(18);
    }

    [Fact]
    public void Create_OnlyUsesCrockfordBase32Alphabet()
    {
        const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
        var suffix = CartId.Create().Value.Split('_')[1];

        foreach (var c in suffix)
        {
            alphabet.ShouldContain(c);
        }
    }

    [Fact]
    public void Create_ProducesLexicographicallyIncreasingIdsAcrossMilliseconds()
    {
        var first = CartId.Create();
        Thread.Sleep(2);
        var second = CartId.Create();

        string.CompareOrdinal(second.Value, first.Value).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Create_GeneratesUniqueIdsAtScale()
    {
        var ids = new HashSet<string>();
        for (var i = 0; i < 10_000; i++)
        {
            ids.Add(CartId.Create().Value).ShouldBeTrue();
        }
    }

    [Fact]
    public void FromString_RoundTripsValidValue()
    {
        var original = CartId.Create();
        var parsed = CartId.FromString(original.Value);

        parsed.Value.ShouldBe(original.Value);
    }

    [Fact]
    public void FromString_ThrowsOnWrongPrefix()
    {
        var id = CartId.Create();
        var swapped = "user_" + id.Value.Split('_')[1];

        Should.Throw<InvalidCastException>(() => CartId.FromString(swapped));
    }

    [Fact]
    public void FromString_ThrowsOnWrongLength()
    {
        Should.Throw<InvalidCastException>(() => CartId.FromString("cart_abc123"));
    }

    [Fact]
    public void FromString_ThrowsOnCharactersOutsideAlphabet()
    {
        // 'i', 'l', 'o', 'u' are excluded from Crockford Base32.
        Should.Throw<InvalidCastException>(() => CartId.FromString("cart_iiiiiiiiiiiiiiiiii"));
        Should.Throw<InvalidCastException>(() => CartId.FromString("cart_uuuuuuuuuuuuuuuuuu"));
    }

    [Fact]
    public void Empty_ReturnsPrefixUnderscoreEmpty()
    {
        CartId.Empty.Value.ShouldBe("cart_empty");
    }
}
