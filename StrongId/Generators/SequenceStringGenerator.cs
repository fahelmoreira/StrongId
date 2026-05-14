using System.Security.Cryptography;

namespace StrongId.Generators;

internal static class SequenceStringGenerator
{
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
    private const int EncodedLength = 18;

    internal static string Create()
    {
        Span<byte> buffer = stackalloc byte[11];

        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        buffer[0] = (byte)(ms >> 40);
        buffer[1] = (byte)(ms >> 32);
        buffer[2] = (byte)(ms >> 24);
        buffer[3] = (byte)(ms >> 16);
        buffer[4] = (byte)(ms >> 8);
        buffer[5] = (byte)ms;

        RandomNumberGenerator.Fill(buffer.Slice(6, 5));

        return EncodeBase32(buffer);
    }

    internal static bool IsValid(string suffix)
    {
        if (suffix.Length != EncodedLength)
        {
            return false;
        }

        foreach (var c in suffix)
        {
            var lower = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            if (Alphabet.IndexOf(lower) < 0)
            {
                return false;
            }
        }

        return true;
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
}
