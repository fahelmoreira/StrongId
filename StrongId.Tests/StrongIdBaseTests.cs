using System.Reflection;
using System.Text.Json;
using StrongId.Configuration;
using Shouldly;
using Xunit;

namespace StrongId.Tests;

public class StrongIdBaseTests
{
    [Fact]
    public void Create_GeneratesIdWithCorrectPrefix()
    {
        var id = UserId.Create();

        id.Value.ShouldStartWith("user_");
        id.Value.Split('_')[1].Length.ShouldBe(32);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var id1 = UserId.Create();
        var id2 = UserId.Create();

        id1.Value.ShouldNotBe(id2.Value);
    }

    [Fact]
    public void Prefix_ReturnsAttributePrefix()
    {
        UserId.Prefix.ShouldBe("user");
        OrderId.Prefix.ShouldBe("order");
    }

    [Fact]
    public void Prefix_ThrowsWhenAttributeMissing()
    {
        Should.Throw<MissingFieldException>(() => NoPrefixId.Prefix);
    }

    [Fact]
    public void Create_ThrowsWhenAttributeMissing()
    {
        Should.Throw<MissingFieldException>(() => NoPrefixId.Create());
    }

    [Fact]
    public void Empty_ReturnsPrefixUnderscoreEmpty()
    {
        UserId.Empty.Value.ShouldBe("user_empty");
    }

    [Fact]
    public void Empty_WithNoPrefixAttribute_ReturnsUnderscoreEmpty()
    {
        NoPrefixId.Empty.Value.ShouldBe("_empty");
    }

    [Fact]
    public void FromString_RoundTripsValidValue()
    {
        var original = UserId.Create();
        var parsed = UserId.FromString(original.Value);

        parsed.Value.ShouldBe(original.Value);
    }

    [Fact]
    public void FromString_ThrowsOnWrongPrefix()
    {
        var orderId = OrderId.Create();

        var ex = Should.Throw<InvalidCastException>(() => UserId.FromString(orderId.Value));
        ex.Message.ShouldContain("order");
        ex.Message.ShouldContain(nameof(UserId));
    }

    [Fact]
    public void FromString_ThrowsOnEmptyPrefix()
    {
        Should.Throw<InvalidCastException>(() => UserId.FromString("_019df8fe572c79e186922e1a64fd2bbf"));
    }

    [Fact]
    public void FromString_ThrowsOnInvalidHex()
    {
        Should.Throw<InvalidCastException>(() => UserId.FromString("user_notavalidhex"));
    }

    [Fact]
    public void FromString_ThrowsWhenAttributeMissing()
    {
        Should.Throw<MissingFieldException>(() => NoPrefixId.FromString("anything_019df8fe572c79e186922e1a64fd2bbf"));
    }

    [Fact]
    public void TryParse_ReturnsTrueForValid()
    {
        var success = UserId.TryParse("user_019df8fe572c79e186922e1a64fd2bbf", out var result);

        success.ShouldBeTrue();
        result.Value.ShouldBe("user_019df8fe572c79e186922e1a64fd2bbf");
    }

    [Fact]
    public void TryParse_ReturnsFalseForInvalid()
    {
        var success = UserId.TryParse("notvalid", out var result);

        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ReturnsFalseForWrongPrefix()
    {
        var success = UserId.TryParse("order_019df8fe572c79e186922e1a64fd2bbf", out _);
        success.ShouldBeFalse();
    }

    [Fact]
    public void Equals_TrueForSameValue()
    {
        var v = "user_019df8fe572c79e186922e1a64fd2bbf";
        var a = UserId.FromString(v);
        var b = UserId.FromString(v);

        a.Equals(b).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_FalseForDifferentValue()
    {
        var a = UserId.Create();
        var b = UserId.Create();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_FalseForNull()
    {
        var a = UserId.Create();

        a.Equals(null).ShouldBeFalse();
        a.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquality_HandlesNulls()
    {
        UserId? a = null;
        UserId? b = null;

        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();

        b = UserId.Create();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_EqualForSameValue()
    {
        var v = "user_019df8fe572c79e186922e1a64fd2bbf";
        UserId.FromString(v).GetHashCode().ShouldBe(UserId.FromString(v).GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = UserId.Create();
        id.ToString().ShouldBe(id.Value);
    }

    [Fact]
    public void Validate_NoErrorsForValidPrefix()
    {
        var id = UserId.Create();
        var results = id.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(id)).ToList();
        results.ShouldBeEmpty();
    }

    [Fact]
    public void SourceGenerator_EmitsPrivateParameterlessConstructor()
    {
        var ctors = typeof(UserId).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var parameterless = ctors.Single(c => c.GetParameters().Length == 0);
        parameterless.IsPrivate.ShouldBeTrue();
    }

    [Fact]
    public void NewKeyword_NotUsableExternally()
    {
        // External `new UserId()` is a compile error (private ctor) — verified at compile time.
        // Confirm at runtime that no public parameterless ctor exists.
        var publicCtor = typeof(UserId).GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
        publicCtor.ShouldBeNull();
    }

    [Fact]
    public void SourceGenerator_ImplementsIStrongIdFactory()
    {
        typeof(StrongId.Interfaces.IStrongIdFactory<UserId>).IsAssignableFrom(typeof(UserId)).ShouldBeTrue();
    }

    private const string Hex = "019df8fe572c79e186922e1a64fd2bbf";

    [Theory]
    [InlineData("user_" + Hex + "_anything")]
    [InlineData("user_" + Hex + "_")]
    [InlineData("user__" + Hex)]
    [InlineData("user_{" + Hex + "}")]
    [InlineData("user_019df8fe-572c-79e1-8692-2e1a64fd2bbf")]
    [InlineData("user_ " + Hex)]
    public void FromString_RejectsNonCanonicalShapes(string value)
    {
        Should.Throw<InvalidCastException>(() => UserId.FromString(value));
        UserId.TryParse(value, out _).ShouldBeFalse();
    }

    [Fact]
    public void FromString_NormalisesUppercaseHexToCanonicalForm()
    {
        var parsed = UserId.FromString("user_" + Hex.ToUpperInvariant());

        parsed.Value.ShouldBe("user_" + Hex);
        parsed.ShouldBe(UserId.FromString("user_" + Hex));
    }

    [Fact]
    public void FromString_KeepsCanonicalInputInstance()
    {
        var value = "user_" + Hex;
        ReferenceEquals(UserId.FromString(value).Value, value).ShouldBeTrue();
    }

    [Theory]
    [InlineData("user")]
    [InlineData("user_")]
    [InlineData("")]
    [InlineData("_")]
    public void FromString_ThrowsInvalidCastForMalformedInput(string value)
    {
        Should.Throw<InvalidCastException>(() => UserId.FromString(value));
        UserId.TryParse(value, out _).ShouldBeFalse();
    }

    [Fact]
    public void FromString_ThrowsArgumentNullForNull()
    {
        Should.Throw<ArgumentNullException>(() => UserId.FromString(null!));
        UserId.TryParse(null!, out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void FromString_ReportsTheBadSuffixInTheMessage()
    {
        var ex = Should.Throw<InvalidCastException>(() => UserId.FromString("user_nothex"));
        ex.Message.ShouldContain("nothex");
    }

    [Fact]
    public void FromString_IgnoreSuffixValidation_StillRejectsBadStructure()
    {
        var original = StrongIdDefaults.Options.IgnoreSuffixValidation;
        StrongIdDefaults.Configure(o => o.IgnoreSuffixValidation = true);
        try
        {
            UserId.FromString("user_anything").Value.ShouldBe("user_anything");
            Should.Throw<InvalidCastException>(() => UserId.FromString("user"));
            Should.Throw<InvalidCastException>(() => UserId.FromString("user_"));
            Should.Throw<InvalidCastException>(() => UserId.FromString("user_admin_" + Hex));
        }
        finally
        {
            StrongIdDefaults.Configure(o => o.IgnoreSuffixValidation = original);
        }
    }

    [Fact]
    public void PrefixWithUnderscore_RoundTrips()
    {
        var id = AdminUserId.Create();

        id.Value.ShouldStartWith("user_admin_");
        AdminUserId.FromString(id.Value).ShouldBe(id);
        id.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(id)).ShouldBeEmpty();
        id.Uuid.ToString("N").ShouldBe(id.Value.Substring("user_admin_".Length));
        AdminUserId.FromUuid(id.Uuid).ShouldBe(id);
    }

    [Fact]
    public void PrefixWithUnderscore_IsNotConfusedWithShorterPrefix()
    {
        var admin = AdminUserId.Create();
        var user = UserId.Create();

        var ex = Should.Throw<InvalidCastException>(() => UserId.FromString(admin.Value));
        ex.Message.ShouldContain("user_admin");
        Should.Throw<InvalidCastException>(() => AdminUserId.FromString(user.Value));
        Should.Throw<InvalidCastException>(() => AdminUserId.FromString("user_" + Hex));
    }

    [Fact]
    public void PrefixWithUnderscore_WorksForSequenceString()
    {
        var id = GiftCartId.Create();

        GiftCartId.FromString(id.Value).ShouldBe(id);
        Should.Throw<InvalidCastException>(() => CartId.FromString(id.Value));
    }

    [Fact]
    public void JsonConverter_ThrowsJsonExceptionForMalformedId()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserId>("\"user\""));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserId>("\"order_" + Hex + "\""));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserId>("\"user_" + Hex + "_x\""));
        JsonSerializer.Deserialize<UserId>("null").ShouldBeNull();
        JsonSerializer.Deserialize<UserId>("\"user_" + Hex.ToUpperInvariant() + "\"")!.Value.ShouldBe("user_" + Hex);
    }
}
