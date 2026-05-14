using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongId.Base;
using StrongId.Configuration;
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

    /// <summary>
    /// Picks the value converter automatically based on the resolved
    /// <see cref="StoreType"/> and <see cref="IdType"/> for <typeparamref name="T"/>.
    /// Attribute values take priority over global <see cref="StrongIdConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// Selection rules:
    /// <list type="bullet">
    /// <item><description><see cref="StoreType.String"/> → always uses the string converter.</description></item>
    /// <item><description><see cref="StoreType.NativeType"/> + <see cref="IdType.Uuid7"/>/<see cref="IdType.Uuid4"/> → UUID converter.</description></item>
    /// <item><description><see cref="StoreType.NativeType"/> + <see cref="IdType.SequenceString"/> → string converter.</description></item>
    /// </list>
    /// </remarks>
    public static PropertyBuilder<T> HasAutoConversion<T>(this PropertyBuilder<T> builder)
        where T : StrongIdBase<T>, IStrongIdFactory<T>
    {
        if (StrongIdBase<T>.ResolvedStoreType is StoreType.String)
        {
            return builder.HasConversion(new Converter.ConvertToString<T>());
        }

        return StrongIdBase<T>.ResolvedIdType switch
        {
            IdType.Uuid7 or IdType.Uuid4 => builder.HasConversion(new Converter.ConvertToUUId<T>()),
            IdType.SequenceString => builder.HasConversion(new Converter.ConvertToString<T>()),
            var other => throw new NotSupportedException(
                $"IdType '{other}' is not supported by HasAutoConversion for {typeof(T).Name}.")
        };
    }
}
