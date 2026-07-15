using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Booster
{
    [CreateAssetMenu(
        fileName = "BoosterGuideCatalog",
        menuName = "FoodieMatch/Booster/Booster Guide Catalog")]
    public sealed class BoosterGuideCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BoosterGuideContentEntry> _entries = new();

        public IReadOnlyList<BoosterGuideContentEntry> Entries => _entries;

        public bool TryGet(BoosterType boosterType, out BoosterGuideContentEntry entry)
        {
            if (_entries == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                BoosterGuideContentEntry candidate = _entries[i];

                if (candidate != null && candidate.BoosterType == boosterType)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public static bool TryFromButtonIndex(int buttonIndex, out BoosterType boosterType)
        {
            switch (buttonIndex)
            {
                case 0:
                    boosterType = BoosterType.Plate;
                    return true;
                case 1:
                    boosterType = BoosterType.Storage;
                    return true;
                case 2:
                    boosterType = BoosterType.Swap;
                    return true;
                case 3:
                    boosterType = BoosterType.Fridge;
                    return true;
                default:
                    boosterType = default;
                    return false;
            }
        }
    }
}
