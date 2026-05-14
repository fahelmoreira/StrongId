namespace StrongId.Configuration;

/// <summary>
/// How the strong id is persisted by the EF Core converter.
/// </summary>
public enum StorageFormat
{
    /// <summary>Resolve to the global default configured on <see cref="StrongIdDefaults"/>.</summary>
    Default,

    /// <summary>Use the id scheme's natural storage (Guid for UUID schemes, string for SequenceString).</summary>
    Native,

    /// <summary>Always store the full prefixed string value.</summary>
    String
}
