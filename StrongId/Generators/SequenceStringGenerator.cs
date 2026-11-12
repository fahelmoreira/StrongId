using System.Security.Cryptography;
using System.Text;

namespace StrongId.Generators;

internal static class SequenceStringGenerator
{
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
    private const int EncodedLength = 18;
    private const int BufferLength = 11;
    private const int SignatureLength = 2;
    private const int PayloadLength = BufferLength - SignatureLength;

    /// <summary>
    /// ASCII → 5-bit value lookup for <see cref="Alphabet"/>, case-insensitive.
    /// Unused entries are <c>-1</c>.
    /// </summary>
    private static readonly sbyte[] DecodeTable = BuildDecodeTable();

    private static sbyte[] BuildDecodeTable()
    {
        var table = new sbyte[128];
        for (var i = 0; i < table.Length; i++)
        {
            table[i] = -1;
        }

        for (var i = 0; i < Alphabet.Length; i++)
        {
            var c = Alphabet[i];
            table[c] = (sbyte)i;
            if (c is >= 'a' and <= 'z')
            {
                table[c - 32] = (sbyte)i;
            }
        }

        return table;
    }

    /// <summary>
    /// Encodes a salt into the HMAC key bytes. Callers on hot paths should compute this once
    /// and pass the result to the <c>byte[]</c> overloads instead of re-encoding per call.
    /// </summary>
    internal static byte[]? GetSaltKey(string? salt) => salt is null ? null : Encoding.UTF8.GetBytes(salt);

    internal static string Create() => Create(DateTimeOffset.UtcNow, saltKey: null);

    internal static string Create(string? salt) => Create(DateTimeOffset.UtcNow, GetSaltKey(salt));

    internal static string Create(byte[]? saltKey) => Create(DateTimeOffset.UtcNow, saltKey);

    internal static string Create(DateTimeOffset timestamp, string? salt) => Create(timestamp, GetSaltKey(salt));

    /// <summary>
    /// Generates a SequenceString suffix using <paramref name="timestamp"/> for the
    /// time-sortable prefix instead of <see cref="DateTimeOffset.UtcNow"/>. Use this
    /// when re-mapping historical ids onto a specific moment in time. The randomness
    /// and (optional) salt signature are generated as usual.
    /// </summary>
    internal static string Create(DateTimeOffset timestamp, byte[]? saltKey)
    {
        Span<byte> buffer = stackalloc byte[BufferLength];

        var ms = timestamp.ToUnixTimeMilliseconds();
        buffer[0] = (byte)(ms >> 40);
        buffer[1] = (byte)(ms >> 32);
        buffer[2] = (byte)(ms >> 24);
        buffer[3] = (byte)(ms >> 16);
        buffer[4] = (byte)(ms >> 8);
        buffer[5] = (byte)ms;

        if (saltKey is null)
        {
            RandomNumberGenerator.Fill(buffer.Slice(6, 5));
        }
        else
        {
            // 3 bytes of true randomness, last 2 bytes hold the salt signature.
            RandomNumberGenerator.Fill(buffer.Slice(6, PayloadLength - 6));
            ComputeSignature(saltKey, buffer.Slice(0, PayloadLength), buffer.Slice(PayloadLength, SignatureLength));
        }

        return EncodeBase32(buffer);
    }

    internal static bool IsValid(string suffix) => TryCanonicalize(suffix, saltKey: null, out _);

    internal static bool IsValid(string suffix, string? salt) => TryCanonicalize(suffix, GetSaltKey(salt), out _);

    internal static bool TryCanonicalize(string suffix, string? salt, out string canonical) =>
        TryCanonicalize(suffix, GetSaltKey(salt), out canonical);

    /// <summary>
    /// Validates <paramref name="suffix"/> and returns its canonical spelling: lowercase, with the
    /// two unused trailing bits of the last character cleared. Several spellings decode to the same
    /// bytes, so callers should store <paramref name="canonical"/> rather than the input.
    /// </summary>
    internal static bool TryCanonicalize(string suffix, byte[]? saltKey, out string canonical)
    {
        canonical = string.Empty;

        if (suffix.Length != EncodedLength)
        {
            return false;
        }

        Span<byte> decoded = stackalloc byte[BufferLength];
        if (!TryDecodeBase32(suffix.AsSpan(), decoded))
        {
            return false;
        }

        if (!HasValidSignature(decoded, saltKey))
        {
            return false;
        }

        var encoded = EncodeBase32(decoded);
        canonical = string.Equals(encoded, suffix, StringComparison.Ordinal) ? suffix : encoded;
        return true;
    }

    private static bool HasValidSignature(ReadOnlySpan<byte> decoded, byte[]? saltKey)
    {
        if (saltKey is null)
        {
            return true;
        }

        Span<byte> expected = stackalloc byte[SignatureLength];
        ComputeSignature(saltKey, decoded.Slice(0, PayloadLength), expected);

        for (var i = 0; i < SignatureLength; i++)
        {
            if (decoded[PayloadLength + i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void ComputeSignature(byte[] key, ReadOnlySpan<byte> payload, Span<byte> destination)
    {
#if NET7_0_OR_GREATER
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(key, payload, hash);
#else
        using var hmac = new HMACSHA256(key);
        Span<byte> hash = stackalloc byte[32];
        if (!hmac.TryComputeHash(payload, hash, out _))
        {
            throw new CryptographicException("HMACSHA256 failed to compute the signature.");
        }
#endif
        hash.Slice(0, destination.Length).CopyTo(destination);
    }

    private static string EncodeBase32(ReadOnlySpan<byte> data)
    {
        Span<char> result = stackalloc char[EncodedLength];
        var bitBuffer = 0;
        var bitsLeft = 0;
        var outIndex = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                var index = (bitBuffer >> bitsLeft) & 0x1F;
                result[outIndex++] = Alphabet[index];
            }
        }

        if (bitsLeft > 0)
        {
            var index = (bitBuffer << (5 - bitsLeft)) & 0x1F;
            result[outIndex] = Alphabet[index];
        }

        return new string(result);
    }

    private static bool TryDecodeBase32(ReadOnlySpan<char> suffix, Span<byte> destination)
    {
        if (suffix.Length != EncodedLength || destination.Length != BufferLength)
        {
            return false;
        }

        var bitBuffer = 0;
        var bitsLeft = 0;
        var outIndex = 0;

        foreach (var c in suffix)
        {
            var idx = c < DecodeTable.Length ? DecodeTable[c] : -1;
            if (idx < 0)
            {
                return false;
            }

            bitBuffer = (bitBuffer << 5) | idx;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                destination[outIndex++] = (byte)((bitBuffer >> bitsLeft) & 0xFF);
            }
        }

        return outIndex == BufferLength;
    }
}
