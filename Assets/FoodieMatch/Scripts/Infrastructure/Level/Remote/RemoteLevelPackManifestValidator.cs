using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal sealed class RemoteLevelPackManifestValidator
    {
        private const int SupportedSchemaVersion = 1;
        private const string LocalManifestFileName =
            "pack_manifest.json";

        public bool IsValid(
            RemoteLevelPackManifestDto manifest,
            RemoteLevelPackDto expectedPack)
        {
            if (manifest == null ||
                manifest.SchemaVersion != SupportedSchemaVersion ||
                manifest.PackId != expectedPack.Id ||
                manifest.PackVersion != expectedPack.Version ||
                manifest.Levels == null)
            {
                return false;
            }

            long expectedLevelCount =
                (long)expectedPack.LastLevel.Value -
                expectedPack.FirstLevel.Value +
                1;

            if (manifest.Levels.Count != expectedLevelCount)
            {
                return false;
            }

            return AreLevelsValid(
                manifest.Levels,
                expectedPack.FirstLevel.Value);
        }

        private static bool AreLevelsValid(
            IReadOnlyList<RemoteLevelEntryDto> levels,
            int firstLevel)
        {
            HashSet<string> contentPaths =
                new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < levels.Count; i++)
            {
                RemoteLevelEntryDto level = levels[i];

                if (level == null ||
                    level.Id != firstLevel + i ||
                    !IsDifficultyValid(level.Difficulty) ||
                    !RemoteLevelPathValidator.IsSafeJsonPath(
                        level.ContentPath) ||
                    string.Equals(
                        level.ContentPath,
                        LocalManifestFileName,
                        StringComparison.OrdinalIgnoreCase) ||
                    !contentPaths.Add(level.ContentPath) ||
                    !RemoteLevelFileHash.IsValid(level.Sha256))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDifficultyValid(string difficulty)
        {
            return !string.IsNullOrWhiteSpace(difficulty) &&
                   Enum.TryParse(
                       difficulty,
                       ignoreCase: true,
                       out LevelDifficulty parsedDifficulty) &&
                   Enum.IsDefined(
                       typeof(LevelDifficulty),
                       parsedDifficulty);
        }
    }
}
