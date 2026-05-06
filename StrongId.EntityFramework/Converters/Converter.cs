using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StrongId.Base;
using StrongId.Interfaces;

namespace StrongId.EntityFramework.Converters;

public class Converter
{
    internal class ConvertToUUId<T>() : ValueConverter<T, Guid>(v => v.Uuid, v => StrongIdBase<T>.FromUuid(v))
        where T : StrongIdBase<T>, IStrongIdFactory<T>;
    internal class ConvertToString<T>() : ValueConverter<T, string>(v => v.Value, v => StrongIdBase<T>.FromString(v))
        where T : StrongIdBase<T>, IStrongIdFactory<T>;
}