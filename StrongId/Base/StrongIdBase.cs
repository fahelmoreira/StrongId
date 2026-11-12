using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using StrongId.Attributes;
using StrongId.Configuration;
using StrongId.Generators;
using StrongId.Interfaces;

namespace StrongId.Base;

/// <summary>
/// Represents a base class for strongly-typed identifiers with an internal prefix and GUID-based value.
/// </summary>
/// <typeparam name="T">
/// The type that extends the <see cref="StrongIdBase{T}"/> class. Must implement the
/// <see cref="IStrongId"/> and <see cref="IStrongIdFactory{T}"/> interfaces.
/// </typeparam>
public class StrongIdBase<T> : StrongId, IValidatableObject, IEquatable<T>  where T : IStrongId, IStrongIdFactory<T>
{
    // Static fields initialise in textual order. The attribute, the factory and the empty
    // value must stay above _generatedSalt, which materialises an instance through them.

    /// <summary>
    /// The <see cref="StrongIdPrefixAttribute"/> on <typeparamref name="T"/>, looked up once per
    /// closed type instead of on every <c>Create</c>/<c>FromString</c>/<c>Prefix</c> call.
    /// Global defaults are still read live (see <see cref="ResolvedIdScheme"/>), so
    /// <see cref="StrongIdDefaults.Configure"/> keeps working after the first id is created.
    /// </summary>
    private static readonly StrongIdPrefixAttribute? _prefixAttribute =
        (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

#if !NET7_0_OR_GREATER
    /// <summary>
    /// The convention-based <c>NewInstance(string)</c> factory, bound once as a delegate.
    /// <c>null</c> when the type doesn't declare one; <see cref="NewInstance"/> then throws.
    /// </summary>
    private static readonly Func<string, T>? _factory = CreateFactory();
#endif

    private static readonly string _emptyValue =
        _prefixAttribute is null ? "_empty" : $"{_prefixAttribute.Prefix}_empty";

    public static string Prefix =>
        _prefixAttribute?.Prefix ?? throw new MissingFieldException("Prefix attribute is missing");

    internal static IdScheme ResolvedIdScheme =>
        _prefixAttribute is null || _prefixAttribute.DeclaredIdScheme is IdScheme.Default
            ? StrongIdDefaults.Options.IdScheme
            : _prefixAttribute.DeclaredIdScheme;

    internal static StorageFormat ResolvedStorageFormat =>
        _prefixAttribute is null || _prefixAttribute.DeclaredStorageFormat is StorageFormat.Default
            ? StrongIdDefaults.Options.StorageFormat
            : _prefixAttribute.DeclaredStorageFormat;

    /// <summary>
    /// Per-type salt populated by the source generator when the <c>[StrongIdPrefix]</c>
    /// attribute supplies a non-null <c>salt</c> argument. Set via the same object
    /// initializer pattern as <see cref="StrongId.Value"/> inside the generated
    /// <c>NewInstance</c> factory. <c>null</c> when the type didn't opt in.
    /// </summary>
    protected string? Salt { get; init; }

    // HMAC key bytes for the salt, encoded once per type rather than on every Create/parse.
    private static readonly byte[]? _generatedSaltKey = SequenceStringGenerator.GetSaltKey(ReadSaltFromTemplate());
    private static readonly byte[]? _defaultSaltKey = SequenceStringGenerator.GetSaltKey(
        _prefixAttribute is null ? null : _prefixAttribute.Prefix + typeof(T).Name);

    private static string? ReadSaltFromTemplate()
    {
        // Materialise one throwaway instance via the source-generated factory so the
        // protected Salt populated in its object initializer becomes observable here.
        return Empty is StrongIdBase<T> typed ? typed.Salt : null;
    }

    /// <summary>
    /// Resolves the active salt key for this id type. Per-type opt-in (a non-null
    /// <c>salt</c> on the attribute) wins; otherwise the global
    /// <see cref="StrongIdOptions.UseSalt"/> falls back to <c>prefix + ClassName</c>.
    /// </summary>
    private static byte[]? ResolveSaltKey()
    {
        if (_generatedSaltKey is not null)
        {
            return _generatedSaltKey;
        }

        return StrongIdDefaults.Options.UseSalt ? _defaultSaltKey : null;
    }

    protected StrongIdBase() { }

    private static T NewInstance(string value)
    {
#if NET7_0_OR_GREATER
        return T.NewInstance(value);
#else
        return _factory is null
            ? throw new MissingMethodException(typeof(T).FullName, "NewInstance")
            : _factory(value);
#endif
    }

#if !NET7_0_OR_GREATER
    private static Func<string, T>? CreateFactory()
    {
        var method = typeof(T).GetMethod(
            "NewInstance",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        if (method is null)
        {
            return null;
        }

        if (method.ReturnType == typeof(T))
        {
            return (Func<string, T>)method.CreateDelegate(typeof(Func<string, T>));
        }

        // Hand-written factory with a different (but castable) return type: keep the old reflective call.
        return value => (T)method.Invoke(null, new object[] { value })!;
    }
#endif

    private static Guid CreateUuid7()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Uuid7Generator.Create();
#endif
    }

    public static T Empty => NewInstance(_emptyValue);

    /// <summary>
    /// Creates a new instance of the StrongIdBase using a prefix and a generated UUID.
    /// </summary>
    /// <returns>A new instance of the StrongIdBase with a generated unique identifier.</returns>
    /// <exception cref="MissingFieldException">
    /// Thrown when the prefix attribute is missing for the given type.
    /// </exception>
    public static T Create()
    {
        var prefix = Prefix;
        var scheme = ResolvedIdScheme;

        var value = scheme switch
        {
            IdScheme.Uuid7 => CreateUuid7().ToString("N"),
            IdScheme.Uuid4 => Guid.NewGuid().ToString("N"),
            IdScheme.Int => throw new NotSupportedException("Int scheme is not supported for automatic generation"),
            IdScheme.SequenceString => SequenceStringGenerator.Create(ResolveSaltKey()),
            _ => throw new NotSupportedException($"IdScheme '{scheme}' is not supported.")
        };

        return NewInstance(string.Concat(prefix, "_", value));
    }

    /// <summary>
    /// Creates a new <see cref="IdScheme.SequenceString"/> id using <paramref name="timestamp"/>
    /// for the time-sortable prefix instead of <see cref="DateTimeOffset.UtcNow"/>. Use this
    /// when re-mapping historical ids (e.g. backfilling a column from a legacy created_at).
    /// Only supported for <see cref="IdScheme.SequenceString"/>.
    /// </summary>
    /// <exception cref="MissingFieldException">Thrown when the prefix attribute is missing.</exception>
    /// <exception cref="NotSupportedException">Thrown when the resolved id scheme is not <see cref="IdScheme.SequenceString"/>.</exception>
    internal static T Create(DateTimeOffset timestamp)
    {
        var prefix = Prefix;
        var scheme = ResolvedIdScheme;

        if (scheme is not IdScheme.SequenceString)
        {
            throw new NotSupportedException($"Create(DateTimeOffset) is only supported for IdScheme.SequenceString — '{typeof(T).Name}' resolves to '{scheme}'.");
        }

        var value = SequenceStringGenerator.Create(timestamp, ResolveSaltKey());
        return NewInstance(string.Concat(prefix, "_", value));
    }

    /// <summary>
    /// Creates a new instance of the StrongIdBase from a string.
    /// </summary>
    /// <remarks>
    /// The value must be <c>{prefix}_{suffix}</c>, where the suffix is a single segment that is
    /// valid for the type's <see cref="IdScheme"/>. The suffix is matched case-insensitively and
    /// the returned id always carries the canonical (lowercase) form, so every accepted spelling
    /// of the same id yields the same <see cref="StrongId.Value"/>.
    /// </remarks>
    /// <param name="value">The string representation of the id.</param>
    /// <returns>The parsed id.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="MissingFieldException">Exception throw if the prefix is not available</exception>
    /// <exception cref="InvalidCastException">Thrown when the value is not a valid id for <typeparamref name="T"/>.</exception>
    public static T FromString(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (_prefixAttribute is null)
        {
            throw new MissingFieldException($"Prefix attribute is missing for {typeof(T).Name}");
        }

        return TryParseCore(value, _prefixAttribute.Prefix, out var result) switch
        {
            ParseFailure.None => result,
            ParseFailure.Prefix => throw new InvalidCastException(
                $"The Prefix {DescribeEmpty(ExtractPrefix(value))} is invalid for {typeof(T).Name}"),
            ParseFailure.Suffix => throw new InvalidCastException(
                $"The suffix {DescribeEmpty(ExtractSuffix(value))} is invalid for {typeof(T).Name}"),
            _ => throw new InvalidCastException($"The value {value} is invalid for {typeof(T).Name}")
        };
    }

    private enum ParseFailure
    {
        None,
        Prefix,
        Suffix,
        UnsupportedScheme
    }

    private static bool IsHex32(string value)
    {
        if (value.Length != 32)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (c is not ((>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F')))
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeEmpty(string part) => string.IsNullOrEmpty(part) ? "empty" : part;

    /// <summary>
    /// Validates <paramref name="value"/> without throwing. On success <paramref name="result"/>
    /// holds an id whose value is in canonical form.
    /// </summary>
    private static ParseFailure TryParseCore(string value, string prefix, out T result)
    {
        result = default!;

        // The suffix never contains '_', so the prefix is everything before the last '_'.
        // This keeps prefixes that themselves contain '_' working, and rejects values with
        // extra segments ("user_<guid>_junk") or another type's longer prefix ("user_admin_<guid>").
        var separator = value.LastIndexOf('_');
        if (!PrefixSpan(value, separator).SequenceEqual(prefix.AsSpan()))
        {
            return ParseFailure.Prefix;
        }

        var suffix = separator < 0 ? string.Empty : value.Substring(separator + 1);

        if (suffix.Length == 0)
        {
            return ParseFailure.Suffix;
        }

        if (StrongIdDefaults.Options.IgnoreSuffixValidation)
        {
            result = NewInstance(value);
            return ParseFailure.None;
        }

        string canonicalSuffix;

        switch (ResolvedIdScheme)
        {
            case IdScheme.Uuid7 or IdScheme.Uuid4:
                // Guid.TryParseExact tolerates surrounding whitespace (and, on some runtimes,
                // other layouts), so require exactly 32 hex digits before handing it over.
                if (!IsHex32(suffix) || !Guid.TryParseExact(suffix, "N", out var guid))
                {
                    return ParseFailure.Suffix;
                }

                canonicalSuffix = guid.ToString("N");
                break;

            case IdScheme.SequenceString:
                if (!SequenceStringGenerator.TryCanonicalize(suffix, ResolveSaltKey(), out canonicalSuffix))
                {
                    return ParseFailure.Suffix;
                }

                break;

            default:
                return ParseFailure.UnsupportedScheme;
        }

        result = NewInstance(string.Equals(suffix, canonicalSuffix, StringComparison.Ordinal)
            ? value
            : string.Concat(prefix, "_", canonicalSuffix));
        return ParseFailure.None;
    }

    /// <summary>
    /// Creates a new instance of the StrongIdBase from a GUID.
    /// </summary>
    /// <param name="uuid">The GUID value used to create the StrongIdBase instance.</param>
    /// <returns>A new instance of the StrongIdBase class of type <typeparamref name="T"/>.</returns>
    internal static T FromUuid(Guid uuid)
    {
        return NewInstance($"{Prefix}_{uuid:N}");
    }
    
    /// <summary>
    ///  Tries to parse a string to a StrongIdBase.
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="result">Strong id</param>
    /// <returns><c>true</c> when <paramref name="value"/> is a valid id for <typeparamref name="T"/>.</returns>
    public static bool TryParse(string value, out T result)
    {
        if (value is null || _prefixAttribute is null)
        {
            result = default!;
            return false;
        }

        return TryParseCore(value, _prefixAttribute.Prefix, out result) is ParseFailure.None;
    }

    /// <summary>
    /// Validates the current instance to ensure the prefix in its value matches the required prefix.
    /// </summary>
    /// <param name="validationContext">Provides the context for validation, including information about the object being validated.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> indicating validation errors, if any.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value is null || !PrefixSpan(Value, Value.LastIndexOf('_')).SequenceEqual(Prefix.AsSpan()))
        {
            yield return new ValidationResult("Prefix is invalid", [nameof(Value)]);
        }
    }
    
    public bool Equals(T? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        return Value == other.Value;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is T other)
        {
            return Value == other.Value;
        }

        return false;
    }
    
    public static bool operator == (StrongIdBase<T>? id1, StrongIdBase<T>? id2)
    {
        if ( id1 is null)
        {
            return id2 is null;
        }

        return id1.Value.Equals(id2?.Value);
    }
    
    public static bool operator != (StrongIdBase<T>? id1, StrongIdBase<T>? id2)
    {
        return id1?.Value != id2?.Value;
    }
    
    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
/// Represents a strongly-typed identifier with a string-based value and a UUID-derived component.
/// Implements the <see cref="IStrongId"/> interface to support type conversion and strongly-typed identity functionality.
/// </summary>
/// <remarks>
/// The identifier value is expected to follow a specific format that includes a prefix and an underlying GUID component,
/// allowing for efficient parsing and validation of UUID-based identifiers.
/// </remarks>
public class StrongId : TypeConverter, IStrongId
{
    
    public string Value { get; init; } = null!;
    
    internal Guid Uuid
    {
        get
        {
            var separator = Value.LastIndexOf('_');
            var suffix = separator < 0 ? ReadOnlySpan<char>.Empty : Value.AsSpan(separator + 1);
            return Guid.ParseExact(suffix, "N".AsSpan());
        }
    }

    /// <summary>
    /// Allocation-free <see cref="ExtractPrefix"/>: the text before the last <c>'_'</c>
    /// (at <paramref name="separator"/>), or the whole value when there is none.
    /// </summary>
    internal static ReadOnlySpan<char> PrefixSpan(string value, int separator) =>
        separator < 0 ? value.AsSpan() : value.AsSpan(0, separator);

    /// <summary>
    /// Returns everything before the last <c>'_'</c> (the whole value when there is none).
    /// Suffixes never contain <c>'_'</c>, so this is the prefix even when the prefix has one.
    /// </summary>
    internal static string ExtractPrefix(string value)
    {
        var separator = value.LastIndexOf('_');
        return separator < 0 ? value : value.Substring(0, separator);
    }

    /// <summary>
    /// Returns everything after the last <c>'_'</c> (empty when there is none).
    /// </summary>
    internal static string ExtractSuffix(string value)
    {
        var separator = value.LastIndexOf('_');
        return separator < 0 ? string.Empty : value.Substring(separator + 1);
    }
    
    protected StrongId()
    {
    }
    
    
    /// <summary>
    /// Parses a prefixed id string into the concrete id type whose
    /// <see cref="StrongIdPrefixAttribute"/> matches the prefix, applying the same
    /// validation as that type's <c>FromString</c>. The prefix → type map is built once
    /// and cached; assemblies loaded later are picked up on the next unknown prefix.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="v"/> is null.</exception>
    /// <exception cref="InvalidCastException">No id type declares the prefix, or the suffix is invalid for it.</exception>
    /// <exception cref="InvalidOperationException">More than one id type declares the prefix.</exception>
    public static IStrongId Convert(string v) => StrongIdRegistry.Parse(v);

    public TypeCode GetTypeCode()
    {
        throw new NotSupportedException();
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public double ToDouble(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public int ToInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public long ToInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public string ToString(IFormatProvider? provider)
    { 
        return Value;
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException();
    }
    
}