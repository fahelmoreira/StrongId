using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongId.Base;
using StrongId.EntityFramework.Converters;
using StrongId.Interfaces;

namespace StrongId.EntityFramework.Extension;

public static class ConverterExtension
{
    public static PropertyBuilder<T> HasUuIdConversion<T>(this PropertyBuilder<T> builder)
        where T : StrongIdBase<T>, IStrongIdFactory<T>
        => builder.HasConversion(new Converter.ConvertToUUId<T>());

    public static PropertyBuilder<T> HasStringConversion<T>(this PropertyBuilder<T> builder)
        where T : StrongIdBase<T>, IStrongIdFactory<T>
        => builder.HasConversion(new Converter.ConvertToString<T>());
}
