namespace StrongId.Configuration;

/// <summary>
/// Mutable bag of global StrongId settings. Configure via <see cref="StrongIdDefaults.Configure"/>.
/// </summary>
public class StrongIdOptions
{
    /// <summary>
    /// Specifies the default scheme for generating StrongId values. Default is <see cref="IdScheme.Uuid7"/>.
    /// </summary>
    public IdScheme IdScheme { get; set; } = IdScheme.Uuid7;

    /// <summary>
    /// Specifies the storage format for StrongId values. Default is <see cref="StorageFormat.Native"/>.
    /// </summary>
    public StorageFormat StorageFormat { get; set; } = StorageFormat.Native;

    /// <summary>
    /// When <c>true</c>, skips validation of <see cref="StrongIdPrefixAttribute"/> suffix values.
    /// Default is <c>false</c>.
    /// </summary>
    public bool IgnoreSuffixValidation { get; set; } = false;

    /// <summary>
    /// When <c>true</c>, every <see cref="IdScheme.SequenceString"/> id is generated and
    /// validated with a per-type salt (default <c>prefix + ClassName</c>). Equivalent to
    /// setting <c>useSalt: true</c> on every <c>[StrongIdPrefix]</c>. Per-type salts set
    /// via the attribute (<c>useSalt</c> or a non-null <c>salt</c>) always take precedence.
    /// </summary>
    public bool UseSalt { get; set; } = false;
}
