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
    
    
    /// <summary>
    /// The scheme exactly as declared on the attribute (may be <see cref="IdScheme.Default"/>).
    /// <see cref="IdScheme"/> resolves <c>Default</c> against the global options at the moment
    /// the attribute is constructed; callers that cache the attribute use this instead so
    /// later <see cref="StrongIdDefaults.Configure"/> calls are still honoured.
    /// </summary>
    internal IdScheme DeclaredIdScheme { get; } = scheme;

    /// <summary>The storage format exactly as declared (may be <see cref="StorageFormat.Default"/>).</summary>
    internal StorageFormat DeclaredStorageFormat { get; } = storage;

    internal bool UseSalt { get; } = salt is not null;

    /// <summary>
    /// Optional override for the salt value. When <see cref="UseSalt"/> is <c>true</c>
    /// and this is <c>null</c>, the generator uses <c>prefix + ClassName</c> by default.
    /// </summary>
    public string? CustomSalt { get; } = salt;
}
