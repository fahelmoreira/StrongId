using System.ComponentModel;
using System.Globalization;
using StrongId.Base;
using StrongId.Interfaces;

namespace StrongId.Converters;

/// <summary>
/// <see cref="TypeConverter"/> for a strong id, attached to each generated id type
/// so that model binders, configuration providers, designers and other
/// reflection-based converters can round-trip the id to and from its string
/// (or UUID) representation.
/// </summary>
public class IdTypeConverter<T> : TypeConverter
    where T : StrongIdBase<T>, IStrongIdFactory<T>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string)
            || sourceType == typeof(Guid)
            || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string)
            || destinationType == typeof(Guid)
            || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value switch
        {
            null => null,
            string s => StrongIdBase<T>.FromString(s),
            Guid g => StrongIdBase<T>.FromUuid(g),
            _ => base.ConvertFrom(context, culture, value)
        };
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is null)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        if (destinationType == typeof(string) && value is T idForString)
        {
            return idForString.Value;
        }

        if (destinationType == typeof(Guid) && value is T idForGuid)
        {
            return idForGuid.Uuid;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
