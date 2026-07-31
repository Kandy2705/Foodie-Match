using System;
using System.Security.Cryptography;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal static class RemoteLevelFileHash
    {
        private const string HexCharacters = "0123456789abcdef";

        public static bool Matches(byte[] content, string expectedSha256)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] actualHash = sha256.ComputeHash(content);
            char[] characters = new char[actualHash.Length * 2];

            for (int i = 0; i < actualHash.Length; i++)
            {
                characters[i * 2] =
                    HexCharacters[actualHash[i] >> 4];
                characters[i * 2 + 1] =
                    HexCharacters[actualHash[i] & 0x0F];
            }

            return string.Equals(
                new string(characters),
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
