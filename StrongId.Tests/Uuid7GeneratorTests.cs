using Shouldly;
using StrongId.Generators;
using Xunit;

namespace StrongId.Tests;

public class Uuid7GeneratorTests
{
    [Fact]
    public void Create_HasVersionNibble7()
    {
        var guid = Uuid7Generator.Create();
        var hex = guid.ToString("N");

        // RFC 9562: char 12 in the 32-char form is the version nibble.
        hex[12].ShouldBe('7');
    }

    [Fact]
    public void Create_HasRfc4122Variant()
    {
        var guid = Uuid7Generator.Create();
        var hex = guid.ToString("N");

        // RFC 9562: top two bits of the variant nibble (char 16) must be 0b10
        // — so the hex digit is one of {8, 9, a, b}.
        var c = hex[16];
        (c is '8' or '9' or 'a' or 'b').ShouldBeTrue($"unexpected variant nibble '{c}'");
    }

    [Fact]
    public void Create_EncodesUnixMillisecondsInLeading48Bits()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var guid = Uuid7Generator.Create();
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var hex = guid.ToString("N");
        var ms = Convert.ToInt64(hex.Substring(0, 12), 16);

        ms.ShouldBeGreaterThanOrEqualTo(before);
        ms.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ProducesDistinctValues()
    {
        var values = new HashSet<Guid>();
        for (var i = 0; i < 1000; i++)
        {
            values.Add(Uuid7Generator.Create()).ShouldBeTrue();
        }
    }

    [Fact]
    public void Create_IsMonotonicAcrossMilliseconds()
    {
        var first = Uuid7Generator.Create();
        Thread.Sleep(2);
        var second = Uuid7Generator.Create();

        var firstMs = Convert.ToInt64(first.ToString("N").Substring(0, 12), 16);
        var secondMs = Convert.ToInt64(second.ToString("N").Substring(0, 12), 16);

        secondMs.ShouldBeGreaterThan(firstMs);
    }
}
