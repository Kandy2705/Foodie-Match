using System;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal static class RemoteLevelPathValidator
    {
        public static bool IsSafeJsonPath(string relativePath)
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
                ".json",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
