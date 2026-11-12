using System.Text.Json;
using System.Text.Json.Serialization;
using StrongId.Base;
using StrongId.Interfaces;

namespace StrongId.Converters;

public class StrongIdJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var type = typeToConvert;
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StrongIdBase<>))
            {
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrongIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class StrongIdJsonConverter<T> : JsonConverter<T>
    where T : IStrongId, IStrongIdFactory<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        var value = reader.GetString();
        if (value is null)
        {
            return default;
        }

        try
        {
            return StrongIdBase<T>.FromString(value);
        }
        catch (InvalidCastException ex)
        {
            // JsonException is what System.Text.Json callers (and ASP.NET Core model binding,
            // which turns it into a 400) expect for malformed input.
            throw new JsonException(ex.Message, ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(((StrongId.Base.StrongId)(object)value).Value);
    }
}
