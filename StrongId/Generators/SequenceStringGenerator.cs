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

    internal static string Create() => Create(salt: null);

    internal static string Create(string? salt)
    {
        Span<byte> buffer = stackalloc byte[BufferLength];

        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        buffer[0] = (byte)(ms >> 40);
        buffer[1] = (byte)(ms >> 32);
        buffer[2] = (byte)(ms >> 24);
        buffer[3] = (byte)(ms >> 16);
        buffer[4] = (byte)(ms >> 8);
        buffer[5] = (byte)ms;

        if (salt is null)
        {
            RandomNumberGenerator.Fill(buffer.Slice(6, 5));
        }
        else
        {
            // 3 bytes of true randomness, last 2 bytes hold the salt signature.
            RandomNumberGenerator.Fill(buffer.Slice(6, PayloadLength - 6));
            ComputeSignature(salt, buffer.Slice(0, PayloadLength), buffer.Slice(PayloadLength, SignatureLength));
        }

        return EncodeBase32(buffer);
    }

    internal static bool IsValid(string suffix) => IsValid(suffix, salt: null);

    internal static bool IsValid(string suffix, string? salt)
    {
        if (suffix.Length != EncodedLength)
        {
            return false;
        }

        Span<byte> decoded = stackalloc byte[BufferLength];
        if (!TryDecodeBase32(suffix, decoded))
        {
            return false;
        }

        if (salt is null)
        {
            return true;
        }

        Span<byte> expected = stackalloc byte[SignatureLength];
        ComputeSignature(salt, decoded.Slice(0, PayloadLength), expected);

        for (var i = 0; i < SignatureLength; i++)
        {
            if (decoded[PayloadLength + i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void ComputeSignature(string salt, ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        var key = Encoding.UTF8.GetBytes(salt);
#if NET7_0_OR_GREATER
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(key, payload, hash);
#else
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(payload.ToArray());
#endif
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] = hash[i];
        }
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

    private static bool TryDecodeBase32(string suffix, Span<byte> destination)
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
            var lower = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            var idx = Alphabet.IndexOf(lower);
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
