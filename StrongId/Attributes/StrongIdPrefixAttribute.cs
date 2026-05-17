using StrongId.Configuration;

namespace StrongId.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StrongIdPrefixAttribute(
    string prefix,
    IdScheme scheme = default,
    StorageFormat storage = default,
    string? salt = null) : Attribute
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
    
    
    internal bool UseSalt { get; } = salt is not null;

    /// <summary>
    /// Optional override for the salt value. When <see cref="UseSalt"/> is <c>true</c>
    /// and this is <c>null</c>, the generator uses <c>prefix + ClassName</c> by default.
    /// </summary>
    public string? CustomSalt { get; } = salt;
}
