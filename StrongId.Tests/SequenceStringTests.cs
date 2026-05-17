using System.Reflection;
using Shouldly;
using StrongId.Configuration;
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

    [Fact]
    public void Salted_AttributeSalt_PopulatesProtectedSaltProperty()
    {
        ReadGeneratedSalt(typeof(SaltedId)).ShouldBe("salted-salt-value");
    }

    [Fact]
    public void Salted_CustomSalt_OverridesAnyDefault()
    {
        ReadGeneratedSalt(typeof(CustomSaltId)).ShouldBe("my-custom-salt");
    }

    [Fact]
    public void Salt_Property_LivesOnStrongIdBase_AndIsProtected()
    {
        // The Salt property is declared on StrongIdBase<T>, not on the generated partial,
        // and exposes a protected getter/init — the source generator only sets it via the
        // object initializer in NewInstance (same pattern as Value).
        var prop = typeof(SaltedId).GetProperty("Salt", BindingFlags.NonPublic | BindingFlags.Instance);
        prop.ShouldNotBeNull();
        prop!.GetMethod!.IsFamily.ShouldBeTrue();              // protected get
        prop.SetMethod!.IsFamily.ShouldBeTrue();               // protected init
        prop.DeclaringType!.IsGenericType.ShouldBeTrue();      // declared on StrongIdBase<T>
        prop.DeclaringType.GetGenericTypeDefinition().Name.ShouldStartWith("StrongIdBase");
    }

    [Fact]
    public void Unsalted_SaltPropertyValueIsNull()
    {
        // CartId opts out (no `salt:` arg), so the generated NewInstance does not set Salt.
        ReadGeneratedSalt(typeof(CartId)).ShouldBeNull();
    }

    [Fact]
    public void Salted_Create_RoundTripsThroughFromString()
    {
        var id = SaltedId.Create();
        var parsed = SaltedId.FromString(id.Value);
        parsed.Value.ShouldBe(id.Value);
    }

    [Fact]
    public void Salted_CustomSalt_RoundTrips()
    {
        var id = CustomSaltId.Create();
        var parsed = CustomSaltId.FromString(id.Value);
        parsed.Value.ShouldBe(id.Value);
    }

    [Fact]
    public void Salted_CrossTypeForgery_IsRejected()
    {
        // Same prefix, different salt — TypeA's suffix should not validate for TypeB.
        var a = ShareTypeAId.Create();
        var forged = a.Value; // already "share_<suffix>", which is the same prefix for TypeB.

        Should.Throw<InvalidCastException>(() => ShareTypeBId.FromString(forged));
    }

    [Fact]
    public void Salted_TamperedSuffix_IsRejected()
    {
        var id = SaltedId.Create();
        var chars = id.Value.ToCharArray();
        // The last char encodes 3 meaningful bits + 2 padding bits, so flipping it
        // is not always observable. Mutate a char in the random region (chars ~9-12)
        // which is always meaningful and always feeds the HMAC input.
        var pos = "salted_".Length + 10;
        chars[pos] = chars[pos] == '0' ? '1' : '0';
        var tampered = new string(chars);

        Should.Throw<InvalidCastException>(() => SaltedId.FromString(tampered));
    }

    [Fact]
    public void Salted_IgnoreSuffixValidation_SkipsSignatureCheck()
    {
        var a = ShareTypeAId.Create();
        var forged = a.Value;

        var original = StrongIdDefaults.Options.IgnoreSuffixValidation;
        StrongIdDefaults.Configure(o => o.IgnoreSuffixValidation = true);
        try
        {
            var parsed = ShareTypeBId.FromString(forged);
            parsed.Value.ShouldBe(forged);
        }
        finally
        {
            StrongIdDefaults.Configure(o => o.IgnoreSuffixValidation = original);
        }
    }

    [Fact]
    public void Salted_Create_StillProducesLexicographicallyIncreasingIds()
    {
        var first = SaltedId.Create();
        Thread.Sleep(2);
        var second = SaltedId.Create();

        string.CompareOrdinal(second.Value, first.Value).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Salted_Create_StaysWithinAlphabetAndLength()
    {
        const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
        var suffix = SaltedId.Create().Value.Split('_')[1];

        suffix.Length.ShouldBe(18);
        foreach (var c in suffix)
        {
            alphabet.ShouldContain(c);
        }
    }

    [Fact]
    public void Global_UseSalt_AppliesToOtherwiseUnsaltedTypes()
    {
        // GloballySaltableId has no salt arg, so the generated NewInstance leaves Salt null.
        ReadGeneratedSalt(typeof(GloballySaltableId)).ShouldBeNull();

        WithGlobalUseSalt(true, () =>
        {
            // With the global flag on, generated ids must validate signature on round-trip.
            var id = GloballySaltableId.Create();
            GloballySaltableId.FromString(id.Value).Value.ShouldBe(id.Value);

            // An 18-char base32 suffix from an unsalted CartId, re-prefixed as "global_", must fail.
            var forged = $"global_{CartId.Create().Value.Split('_')[1]}";
            Should.Throw<InvalidCastException>(() => GloballySaltableId.FromString(forged));
        });
    }

    [Fact]
    public void Global_UseSalt_OffByDefault()
    {
        // Sanity: with no global flag and no attribute opt-in, behaviour is unchanged.
        StrongIdDefaults.Options.UseSalt.ShouldBeFalse();

        var id = GloballySaltableId.Create();
        GloballySaltableId.FromString(id.Value).Value.ShouldBe(id.Value);

        // Re-prefixed cart suffix passes (no salt validation).
        var reprefixed = $"global_{CartId.Create().Value.Split('_')[1]}";
        GloballySaltableId.FromString(reprefixed).Value.ShouldBe(reprefixed);
    }

    [Fact]
    public void Global_UseSalt_DoesNotOverridePerTypeCustomSalt()
    {
        // Generate a CustomSaltId with the global flag off — it uses "my-custom-salt".
        var pristine = CustomSaltId.Create();

        WithGlobalUseSalt(true, () =>
        {
            // Even with the global flag on, the per-type custom salt continues to validate.
            CustomSaltId.FromString(pristine.Value).Value.ShouldBe(pristine.Value);

            // And new ids still use the custom salt (validatable across global toggles).
            var fresh = CustomSaltId.Create();
            CustomSaltId.FromString(fresh.Value).Value.ShouldBe(fresh.Value);
        });

        // With the flag off again, the same id continues to validate.
        CustomSaltId.FromString(pristine.Value).Value.ShouldBe(pristine.Value);
    }

    [Fact]
    public void Global_UseSalt_DefaultSaltMatchesPrefixPlusClassName()
    {
        WithGlobalUseSalt(true, () =>
        {
            var id = GloballySaltableId.Create();
            var suffix = id.Value.Split('_')[1];

            // Validating with the exact "prefix+ClassName" salt must succeed.
            StrongId.Generators.SequenceStringGenerator
                .IsValid(suffix, "globalGloballySaltableId").ShouldBeTrue();

            // And a different salt must reject it.
            StrongId.Generators.SequenceStringGenerator
                .IsValid(suffix, "some-other-salt").ShouldBeFalse();
        });
    }

    private static void WithGlobalUseSalt(bool value, Action body)
    {
        var original = StrongIdDefaults.Options.UseSalt;
        StrongIdDefaults.Configure(o => o.UseSalt = value);
        try
        {
            body();
        }
        finally
        {
            StrongIdDefaults.Configure(o => o.UseSalt = original);
        }
    }

    // Salt is a protected instance property on StrongIdBase<T>, populated by the
    // source-generated NewInstance factory. Tests can only observe it via reflection,
    // and the lookup has to walk the inheritance chain because both Empty (static)
    // and Salt (non-public instance) are declared on the generic base.
    private static string? ReadGeneratedSalt(Type t)
    {
        var emptyProp = FindInheritedProperty(t, "Empty", BindingFlags.Public | BindingFlags.Static);
        var empty = emptyProp?.GetValue(null);
        if (empty is null) return null;
        var saltProp = FindInheritedProperty(t, "Salt", BindingFlags.NonPublic | BindingFlags.Instance);
        return saltProp?.GetValue(empty) as string;
    }

    private static PropertyInfo? FindInheritedProperty(Type? type, string name, BindingFlags flags)
    {
        while (type is not null)
        {
            var prop = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (prop is not null) return prop;
            type = type.BaseType;
        }
        return null;
    }
}
