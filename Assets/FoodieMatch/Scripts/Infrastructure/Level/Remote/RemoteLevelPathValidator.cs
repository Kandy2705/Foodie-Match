using System;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal static class RemoteLevelPathValidator
    {
        public static bool IsSafeJsonPath(string relativePath)
        {
            return IsSafePath(relativePath, ".json");
        }

        public static bool IsSafeZipPath(string relativePath)
        {
            return IsSafePath(relativePath, ".zip");
        }

        private static bool IsSafePath(
            string relativePath,
            string extension)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                relativePath.StartsWith("/", StringComparison.Ordinal) ||
                relativePath.Contains("\\") ||
                relativePath.Contains(":") ||
                relativePath.Contains("?") ||
                relativePath.Contains("#"))
            {
                return false;
            }

            string[] pathParts = relativePath.Split('/');

            for (int i = 0; i < pathParts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(pathParts[i]) ||
                    pathParts[i] == "." ||
                    pathParts[i] == "..")
                {
                    return false;
                }
            }

            return relativePath.EndsWith(
                extension,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
