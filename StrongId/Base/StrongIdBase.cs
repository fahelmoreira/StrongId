using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using StrongId.Attributes;
using StrongId.Interfaces;

namespace StrongId.Base;

public class StrongIdBase<T> : StrongId, IValidatableObject,IEquatable<T>  where T : IStrongId, IStrongIdFactory<T>
{
    public static string Prefix
    {
        get
        {
            var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

            return prefixAttribute is null ? throw new MissingFieldException("Prefix attribute is missing") : prefixAttribute.Prefix;
        }
    }

    protected StrongIdBase() { }

    public static T Empty => T.NewInstance(
        (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute)) is null
            ? "_empty"
            : $"{((StrongIdPrefixAttribute?) Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute)))!.Prefix}_empty");

    public static T Create()
    {
        var prefixAttribute = (StrongIdPrefixAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(StrongIdPrefixAttribute));

        if(prefixAttribute is null)
        {
            throw new MissingFieldException("Prefix attribute is missing");
        }

        var guid = $"{Guid.CreateVersion7():N}";

        return T.NewInstance($"{prefixAttribute.Prefix}_{guid}");
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

        var hex = value.Split("_")[1];
        
        if(!Guid.TryParseExact(hex, "N", out var _))
        {
            throw new InvalidCastException($"The hex {(string.IsNullOrEmpty(hex) ? "empty" : prefix)} is invalid for {typeof(T).Name}");
        }
        
        return T.NewInstance(value);
    }

    internal static T FromUuid(Guid uuid)
    {
        return T.NewInstance($"{Prefix}_{uuid:N}");
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