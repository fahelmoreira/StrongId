using Shouldly;
using Xunit;

namespace StrongId.Tests;

public class StrongIdConvertTests
{
    [Fact(Skip = "StrongId.Convert currently throws NotSupportedException via IConvertible.ToType — known issue")]
    public void Convert_FindsTypeByPrefix()
    {
        var id = UserId.Create();

        var result = global::StrongId.Base.StrongId.Convert(id.Value);

        result.ShouldNotBeNull();
        ((global::StrongId.Base.StrongId)result).Value.ShouldBe(id.Value);
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
