namespace StrongId.Configuration;

/// <summary>
/// Global defaults applied to any <c>[StrongIdPrefix]</c> that doesn't specify an
/// <see cref="IdScheme"/> or <see cref="StorageFormat"/>.
/// </summary>
public static class StrongIdDefaults
{
    private const IdScheme FallbackIdScheme = IdScheme.Uuid7;
    private const StorageFormat FallbackStorageFormat = StorageFormat.Native;

    internal static StrongIdOptions Options { get; } = new()
    {
        IdScheme = FallbackIdScheme,
        StorageFormat = FallbackStorageFormat
    };

    public static void Configure(Action<StrongIdOptions> options)
    {
        options.Invoke(Options);

        Options.IdScheme = Options.IdScheme switch
        {
            IdScheme.Default => FallbackIdScheme,
            _ => Options.IdScheme
        };

        Options.StorageFormat = Options.StorageFormat switch
        {
            StorageFormat.Default => FallbackStorageFormat,
            _ => Options.StorageFormat
        };
    }
}
