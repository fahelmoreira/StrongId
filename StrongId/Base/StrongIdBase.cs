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
    public static string Prefix
    {
        get
        {
            var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

            return prefixAttribute is null ? throw new MissingFieldException("Prefix attribute is missing") : prefixAttribute.Prefix;
        }
    }
    
    internal static IdScheme ResolvedIdScheme
    {
        get
        {
            var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

            return prefixAttribute is null || prefixAttribute.IdScheme is IdScheme.Default
                ? StrongIdDefaults.Options.IdScheme
                : prefixAttribute.IdScheme;
        }
    }

    internal static StorageFormat ResolvedStorageFormat
    {
        get
        {
            var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

            return prefixAttribute is null || prefixAttribute.StorageFormat is StorageFormat.Default
                ? StrongIdDefaults.Options.StorageFormat
                : prefixAttribute.StorageFormat;
        }
    }

    protected StrongIdBase() { }

    private static T NewInstance(string value)
    {
#if NET7_0_OR_GREATER
        return T.NewInstance(value);
#else
        var method = typeof(T).GetMethod(
            "NewInstance",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        if (method is null)
        {
            throw new MissingMethodException(typeof(T).FullName, "NewInstance");
        }

        return (T)method.Invoke(null, new object[] { value })!;
#endif
    }

    private static Guid CreateUuid7()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Uuid7Generator.Create();
#endif
    }

    public static T Empty => NewInstance(
        (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute)) is null
            ? "_empty"
            : $"{((StrongIdPrefixAttribute?) Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute)))!.Prefix}_empty");

    /// <summary>
    /// Creates a new instance of the StrongIdBase using a prefix and a generated UUID.
    /// </summary>
    /// <returns>A new instance of the StrongIdBase with a generated unique identifier.</returns>
    /// <exception cref="MissingFieldException">
    /// Thrown when the prefix attribute is missing for the given type.
    /// </exception>
    public static T Create()
    {
        var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

        if(prefixAttribute is null)
        {
            throw new MissingFieldException("Prefix attribute is missing");
        }


        var value = ResolvedIdScheme switch
        {
            IdScheme.Uuid7 => $"{CreateUuid7():N}",
            IdScheme.Uuid4 => $"{Guid.NewGuid():N}",
            IdScheme.Int => throw new NotSupportedException("Int scheme is not supported for automatic generation"),
            IdScheme.SequenceString => SequenceStringGenerator.Create(),
            _ => throw new NotSupportedException($"IdScheme '{ResolvedIdScheme}' is not supported.")
        };

        return NewInstance($"{prefixAttribute.Prefix}_{value}");
    }
    
    /// <summary>
    /// Creates a new instance of the StrongIdBase from a string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="MissingFieldException">Exception throw if the prefix is not available</exception>
    /// <exception cref="InvalidCastException">Exception throw if the UUID is not valid </exception>
    public static T FromString(string value)
    {
        var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));
        
        
        if(prefixAttribute is null)
        {
            throw new MissingFieldException($"Prefix attribute is missing for {typeof(T).Name}");
        }
        
        var prefix = value.Split("_")[0];

        if (prefix != prefixAttribute.Prefix)
        {
            throw new InvalidCastException($"The Prefix {(string.IsNullOrEmpty(prefix) ? "empty" : prefix)} is invalid for {typeof(T).Name}");
        }

        if (ResolvedIdScheme is IdScheme.Uuid7 or IdScheme.Uuid4)
        {
            var hex = value.Split("_")[1];

            if(!Guid.TryParseExact(hex, "N", out var _))
            {
                throw new InvalidCastException($"The hex {(string.IsNullOrEmpty(hex) ? "empty" : prefix)} is invalid for {typeof(T).Name}");
            }

            return NewInstance(value);
        }

        if (ResolvedIdScheme is IdScheme.SequenceString)
        {
            var suffix = value.Split("_")[1];

            if (!SequenceStringGenerator.IsValid(suffix))
            {
                throw new InvalidCastException($"The suffix {(string.IsNullOrEmpty(suffix) ? "empty" : suffix)} is invalid for {typeof(T).Name}");
            }

            return NewInstance(value);
        }

        throw new InvalidCastException($"The value {value} is invalid for {typeof(T).Name}");
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
    /// <returns></returns>
    public static bool TryParse(string value, out T result)
    {
        try
        {
            result = FromString(value);
            return true;
        }
        catch (Exception)
        {
            result = default!;
            return false;
        }
    }

    /// <summary>
    /// Validates the current instance to ensure the prefix in its value matches the required prefix.
    /// </summary>
    /// <param name="validationContext">Provides the context for validation, including information about the object being validated.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> indicating validation errors, if any.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var prefix = Value.Split("_")[0];

        if (prefix != Prefix)
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
    
    internal Guid Uuid => Guid.ParseExact(Value.Split("_")[1], "N");
    
    private StrongId(string value) : this()
    {
        Value = value;
    }
    
    protected StrongId()
    {
    }
    
    
    public static IStrongId Convert(string v)
    {
        IStrongId key = default!;
        var prefix = v.Split("_")[0];
        var domainKeyInterfaces = typeof(IStrongId);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes()).ToList()
            .Where(p => domainKeyInterfaces.IsAssignableFrom(p) && p != domainKeyInterfaces).ToList();
                
        foreach (var type in types)
        {
            var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(type, typeof(StrongIdPrefixAttribute));
            
            if(prefixAttribute?.Prefix == prefix)
            {
                var instance =  new StrongId(v);
                key = (IStrongId)System.Convert.ChangeType(instance, type);
            }
        }

        return key;
    }

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