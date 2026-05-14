using System.ComponentModel;
using System.Globalization;
using Shouldly;
using StrongId.Converters;
using Xunit;

namespace StrongId.Tests;

public class IdTypeConverterTests
{
    [Fact]
    public void SourceGenerator_AttachesIdTypeConverter()
    {
        var attribute = (TypeConverterAttribute?)Attribute.GetCustomAttribute(typeof(UserId), typeof(TypeConverterAttribute));

        attribute.ShouldNotBeNull();
        attribute.ConverterTypeName.ShouldContain("IdTypeConverter");
        attribute.ConverterTypeName.ShouldContain("UserId");
    }

    [Fact]
    public void TypeDescriptor_ResolvesIdTypeConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(UserId));

        converter.ShouldBeOfType<IdTypeConverter<UserId>>();
    }

    [Fact]
    public void ConvertFromString_ReturnsStrongId()
    {
        var converter = new IdTypeConverter<UserId>();

        var result = converter.ConvertFromString("user_019df8fe572c79e186922e1a64fd2bbf");

        result.ShouldBeOfType<UserId>();
        ((UserId)result!).Value.ShouldBe("user_019df8fe572c79e186922e1a64fd2bbf");
    }

    [Fact]
    public void ConvertToString_ReturnsValue()
    {
        var converter = new IdTypeConverter<UserId>();
        var id = UserId.FromString("user_019df8fe572c79e186922e1a64fd2bbf");

        var result = converter.ConvertToString(id);

        result.ShouldBe("user_019df8fe572c79e186922e1a64fd2bbf");
    }

    [Fact]
    public void CanConvertFromString()
    {
        var converter = new IdTypeConverter<UserId>();

        converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
    }

    [Fact]
    public void CanConvertToString()
    {
        var converter = new IdTypeConverter<UserId>();

        converter.CanConvertTo(typeof(string)).ShouldBeTrue();
    }

    [Fact]
    public void ConvertFrom_ThrowsOnWrongPrefix()
    {
        var converter = new IdTypeConverter<UserId>();

        Should.Throw<InvalidCastException>(
            () => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "order_019df8fe572c79e186922e1a64fd2bbf"));
    }

    [Fact]
    public void CanConvertFromAndToGuid()
    {
        var converter = new IdTypeConverter<UserId>();

        converter.CanConvertFrom(typeof(Guid)).ShouldBeTrue();
        converter.CanConvertTo(typeof(Guid)).ShouldBeTrue();
    }

    [Fact]
    public void ConvertFromGuid_ProducesPrefixedId()
    {
        var converter = new IdTypeConverter<UserId>();
        var guid = Guid.Parse("019df8fe-572c-79e1-8692-2e1a64fd2bbf");

        var result = (UserId?)converter.ConvertFrom(null, CultureInfo.InvariantCulture, guid);

        result.ShouldNotBeNull();
        result.Value.ShouldBe("user_019df8fe572c79e186922e1a64fd2bbf");
    }

    [Fact]
    public void ConvertToGuid_ExtractsUuid()
    {
        var converter = new IdTypeConverter<UserId>();
        var id = UserId.FromString("user_019df8fe572c79e186922e1a64fd2bbf");

        var result = (Guid)converter.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(Guid))!;

        result.ShouldBe(Guid.Parse("019df8fe-572c-79e1-8692-2e1a64fd2bbf"));
    }
}
