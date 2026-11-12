using Shouldly;
using Xunit;

namespace StrongId.Tests;

public class StrongIdConvertTests
{
    [Fact]
    public void Convert_FindsTypeByPrefix()
    {
        var id = UserId.Create();

        var result = global::StrongId.Base.StrongId.Convert(id.Value);

        var typed = result.ShouldBeOfType<UserId>();
        typed.ShouldBe(id);
        typed.Value.ShouldBe(id.Value);
    }

    [Fact]
    public void Convert_ResolvesEachRegisteredType()
    {
        var order = OrderId.Create();
        var cart = CartId.Create();

        global::StrongId.Base.StrongId.Convert(order.Value).ShouldBeOfType<OrderId>().ShouldBe(order);
        global::StrongId.Base.StrongId.Convert(cart.Value).ShouldBeOfType<CartId>().ShouldBe(cart);
    }

    [Fact]
    public void Convert_RepeatedCalls_ReturnEqualIds()
    {
        var id = UserId.Create();

        var first = global::StrongId.Base.StrongId.Convert(id.Value);
        var second = global::StrongId.Base.StrongId.Convert(id.Value);

        first.ShouldBe(second);
    }

    [Fact]
    public void Convert_UnknownPrefix_Throws()
    {
        Should.Throw<InvalidCastException>(() => global::StrongId.Base.StrongId.Convert("nope_0123456789abcdef0123456789abcdef"));
    }

    [Fact]
    public void Convert_InvalidSuffix_Throws()
    {
        Should.Throw<InvalidCastException>(() => global::StrongId.Base.StrongId.Convert("user_not-a-guid"));
    }

    [Fact]
    public void Convert_AmbiguousPrefix_Throws()
    {
        var id = ShareTypeAId.Create();

        Should.Throw<InvalidOperationException>(() => global::StrongId.Base.StrongId.Convert(id.Value));
    }

    [Fact]
    public void Convert_Null_Throws()
    {
        Should.Throw<ArgumentNullException>(() => global::StrongId.Base.StrongId.Convert(null!));
    }

    [Fact]
    public void ToStringWithFormat_ReturnsValue()
    {
        var id = UserId.Create();
        ((IConvertible)id).ToString(null).ShouldBe(id.Value);
    }

    [Fact]
    public void IConvertible_OtherMethodsThrowNotSupported()
    {
        var id = UserId.Create();
        IConvertible c = id;

        Should.Throw<NotSupportedException>(() => c.ToInt32(null));
        Should.Throw<NotSupportedException>(() => c.ToBoolean(null));
        Should.Throw<NotSupportedException>(() => c.ToDateTime(null));
        Should.Throw<NotSupportedException>(() => c.ToDouble(null));
        Should.Throw<NotSupportedException>(() => c.GetTypeCode());
    }
}
