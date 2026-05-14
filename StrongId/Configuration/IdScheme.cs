namespace StrongId.Configuration;

/// <summary>
/// The algorithm used to generate the value portion of a strong id.
/// </summary>
public enum IdScheme
{
    /// <summary>Resolve to the global default configured on <see cref="StrongIdDefaults"/>.</summary>
    Default,

    /// <summary>Time-sortable UUID v7 (32 hex chars).</summary>
    Uuid7,

    /// <summary>Random UUID v4 (32 hex chars).</summary>
    Uuid4,

    /// <summary>Sequential integer — not currently supported by automatic generation.</summary>
    Int,

    /// <summary>Compact 18-char Crockford-base32 id: 48-bit ms timestamp + 40-bit randomness.</summary>
    SequenceString
}
