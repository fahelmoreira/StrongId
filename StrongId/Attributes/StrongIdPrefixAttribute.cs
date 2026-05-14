using StrongId.Configuration;

namespace StrongId.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StrongIdPrefixAttribute(string prefix, IdType type = default, StoreType store = default) : Attribute
{
    public string Prefix { get; } = prefix;
    public IdType IdType { get; } = type switch{
        IdType.Default => StrongIdConfiguration.ConfigureOptions.IdType,
        _ => type
    };
    public StoreType StoreType { get; } = store switch{
        StoreType.Default => StrongIdConfiguration.ConfigureOptions.StoreType,
        _ => store
    };
}