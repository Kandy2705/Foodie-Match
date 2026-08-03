using System;
using System.Security.Cryptography;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public static class RemoteLevelFileHash
    {
        private const string HexCharacters = "0123456789abcdef";

        public static string Compute(byte[] content)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(content);
            char[] characters = new char[hash.Length * 2];

            for (int i = 0; i < hash.Length; i++)
            {
                characters[i * 2] = HexCharacters[hash[i] >> 4];
                characters[i * 2 + 1] =
                    HexCharacters[hash[i] & 0x0F];
            }

            return new string(characters);
        }

        public static bool Matches(byte[] content, string expectedSha256)
        {
            return string.Equals(
                Compute(content),
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValid(string sha256)
        {
            if (sha256 == null || sha256.Length != 64)
            {
                return false;
            }

            for (int i = 0; i < sha256.Length; i++)
            {
                if (!Uri.IsHexDigit(sha256[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
