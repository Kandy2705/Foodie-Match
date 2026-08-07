using System.Collections.Generic;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.UI.Booster
{
    [CreateAssetMenu(
        fileName = "BoosterBuyCatalog",
        menuName = "FoodieMatch/Booster/Booster Guide Catalog")]
    public sealed class BoosterBuyCatalogSO : ScriptableObject
    {
        [SerializeField] private Sprite _lockedButtonSprite;

        [SerializeField] private List<BoosterBuyContentEntry> _entries = new();

        public Sprite LockedButtonSprite => _lockedButtonSprite;

        public IReadOnlyList<BoosterBuyContentEntry> Entries => _entries;

        public bool TryGet(BoosterType boosterType, out BoosterBuyContentEntry entry)
        {
            if (_entries == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                BoosterBuyContentEntry candidate = _entries[i];

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
                case 4:
                    boosterType = BoosterType.Box;
                    return true;
                default:
                    boosterType = default;
                    return false;
            }
        }

        public static bool TryGetButtonIndex(BoosterType boosterType, out int buttonIndex)
        {
            switch (boosterType)
            {
                case BoosterType.Plate:
                    buttonIndex = 0;
                    return true;
                case BoosterType.Storage:
                    buttonIndex = 1;
                    return true;
                case BoosterType.Swap:
                    buttonIndex = 2;
                    return true;
                case BoosterType.Fridge:
                    buttonIndex = 3;
                    return true;
                default:
                    buttonIndex = -1;
                    return false;
            }
        }
    }
}
