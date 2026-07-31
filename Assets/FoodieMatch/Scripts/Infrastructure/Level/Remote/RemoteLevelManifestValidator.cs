using System;
using System.Collections.Generic;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal sealed class RemoteLevelManifestValidator
    {
        private const int SupportedSchemaVersion = 1;

        public bool IsValid(
            RemoteLevelManifestDto manifest,
            int? expectedManifestVersion = null)
        {
            if (manifest == null ||
                manifest.SchemaVersion != SupportedSchemaVersion ||
                !manifest.ManifestVersion.HasValue ||
                manifest.ManifestVersion.Value <= 0 ||
                manifest.Packs == null ||
                manifest.Packs.Count == 0)
            {
                return false;
            }

            if (expectedManifestVersion.HasValue &&
                manifest.ManifestVersion.Value != expectedManifestVersion.Value)
            {
                return false;
            }

            return ArePacksValid(manifest.Packs);
        }

        private static bool ArePacksValid(
            IReadOnlyList<RemoteLevelPackDto> packs)
        {
            HashSet<int> packIds = new();
            HashSet<string> manifestPaths =
                new(StringComparer.OrdinalIgnoreCase);
            List<RemoteLevelPackDto> orderedPacks = new(packs);

            for (int i = 0; i < packs.Count; i++)
            {
                RemoteLevelPackDto pack = packs[i];

                if (!IsPackValid(pack) ||
                    !packIds.Add(pack.Id.Value) ||
                    !manifestPaths.Add(pack.ManifestPath))
                {
                    return false;
                }
            }

            orderedPacks.Sort(
                (left, right) =>
                    left.FirstLevel.Value.CompareTo(right.FirstLevel.Value));

            for (int i = 1; i < orderedPacks.Count; i++)
            {
                if (orderedPacks[i].FirstLevel.Value <=
                    orderedPacks[i - 1].LastLevel.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPackValid(RemoteLevelPackDto pack)
        {
            return pack != null &&
                   pack.Id.HasValue &&
                   pack.Id.Value > 0 &&
                   pack.Version.HasValue &&
                   pack.Version.Value > 0 &&
                   pack.FirstLevel.HasValue &&
                   pack.FirstLevel.Value > 0 &&
                   pack.LastLevel.HasValue &&
                   pack.LastLevel.Value >= pack.FirstLevel.Value &&
                   RemoteLevelPathValidator.IsSafeJsonPath(
                       pack.ManifestPath);
        }
    }
}
