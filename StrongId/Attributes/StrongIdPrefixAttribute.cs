using StrongId.Configuration;

namespace StrongId.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StrongIdPrefixAttribute(string prefix, IdScheme scheme = default, StorageFormat storage = default) : Attribute
{
    public string Prefix { get; } = prefix;

    public IdScheme IdScheme { get; } = scheme switch
    {
        IdScheme.Default => StrongIdDefaults.Options.IdScheme,
        _ => scheme
    };

    public StorageFormat StorageFormat { get; } = storage switch
    {
        StorageFormat.Default => StrongIdDefaults.Options.StorageFormat,
        _ => storage
    };
    
}
