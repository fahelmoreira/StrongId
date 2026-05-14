namespace StrongId.Configuration;

/// <summary>
/// Mutable bag of global StrongId settings. Configure via <see cref="StrongIdDefaults.Configure"/>.
/// </summary>
public class StrongIdOptions
{
    public IdScheme IdScheme { get; set; } = IdScheme.Uuid7;
    public StorageFormat StorageFormat { get; set; } = StorageFormat.Native;
    public bool IgnoreSuffixValidation { get; set; } = false;

}
