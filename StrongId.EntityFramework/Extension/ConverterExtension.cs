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
    /// <see cref="StorageFormat"/> and <see cref="IdScheme"/> for <typeparamref name="T"/>.
    /// Attribute values take priority over global <see cref="StrongIdDefaults"/>.
    /// </summary>
    /// <remarks>
    /// Selection rules:
    /// <list type="bullet">
    /// <item><description><see cref="StorageFormat.String"/> → always uses the string converter.</description></item>
    /// <item><description><see cref="StorageFormat.Native"/> + <see cref="IdScheme.Uuid7"/>/<see cref="IdScheme.Uuid4"/> → UUID converter.</description></item>
    /// <item><description><see cref="StorageFormat.Native"/> + <see cref="IdScheme.SequenceString"/> → string converter.</description></item>
    /// </list>
    /// </remarks>
    public static PropertyBuilder<T> HasAutoConversion<T>(this PropertyBuilder<T> builder)
        where T : StrongIdBase<T>, IStrongIdFactory<T>
    {
        if (StrongIdBase<T>.ResolvedStorageFormat is StorageFormat.String)
        {
            return builder.HasConversion(new Converter.ConvertToString<T>());
        }

        return StrongIdBase<T>.ResolvedIdScheme switch
        {
            IdScheme.Uuid7 or IdScheme.Uuid4 => builder.HasConversion(new Converter.ConvertToUUId<T>()),
            IdScheme.SequenceString => builder.HasConversion(new Converter.ConvertToString<T>()),
            var other => throw new NotSupportedException(
                $"IdScheme '{other}' is not supported by HasAutoConversion for {typeof(T).Name}.")
        };
    }
}
