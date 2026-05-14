using System.Security.Cryptography;

namespace StrongId.Generators;

/// <summary>
/// RFC 9562 UUID v7 generator used on target frameworks that don't ship
/// <c>Guid.CreateVersion7()</c> natively (everything before .NET 9).
/// </summary>
internal static class Uuid7Generator
{
    internal static Guid Create()
    {
        Span<byte> bytes = stackalloc byte[16];

        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(ms >> 40);
        bytes[1] = (byte)(ms >> 32);
        bytes[2] = (byte)(ms >> 24);
        bytes[3] = (byte)(ms >> 16);
        bytes[4] = (byte)(ms >> 8);
        bytes[5] = (byte)ms;

        RandomNumberGenerator.Fill(bytes.Slice(6, 10));

        // version: top nibble of byte 6 = 0b0111
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        // variant: top two bits of byte 8 = 0b10
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

#if NET8_0_OR_GREATER
        return new Guid(bytes, bigEndian: true);
#else
        // System.Guid's byte ctor reads the first three fields as little-endian
        // even though the canonical UUID textual form is big-endian. Swap the
        // first 8 bytes into Guid layout so ToString("N") matches the UUID v7
        // bit pattern we just built.
        Span<byte> guidBytes = stackalloc byte[16];
        guidBytes[0] = bytes[3];
        guidBytes[1] = bytes[2];
        guidBytes[2] = bytes[1];
        guidBytes[3] = bytes[0];
        guidBytes[4] = bytes[5];
        guidBytes[5] = bytes[4];
        guidBytes[6] = bytes[7];
        guidBytes[7] = bytes[6];
        bytes.Slice(8, 8).CopyTo(guidBytes.Slice(8, 8));
        return new Guid(guidBytes);
#endif
    }
}
