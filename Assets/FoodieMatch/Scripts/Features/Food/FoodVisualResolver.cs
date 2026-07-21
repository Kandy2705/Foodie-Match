using System.Collections.Generic;
using FoodieMatch.Data.Food;
using UnityEngine;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodVisualResolver : MonoBehaviour
    {
        [SerializeField] private FoodVisualCatalogSO _visualCatalog;

        private readonly Dictionary<int, Sprite> _iconByFoodTokenId = new();

        public bool TryCreateMapping(IReadOnlyList<int> foodTokenIds, int seed)
        {
            ClearMapping();

            if (foodTokenIds == null || foodTokenIds.Count == 0)
            {
                Debug.LogError("Food token collection is empty.", this);

                return false;
            }

            if (_visualCatalog == null)
            {
                Debug.LogError("Food visual catalog is missing.", this);

                return false;
            }

            List<int> uniqueFoodTokenIds = GetUniqueFoodTokenIds(foodTokenIds);
            List<Sprite> availableIcons = GetAvailableIcons();

            if (availableIcons.Count < uniqueFoodTokenIds.Count)
            {
                Debug.LogError(
                    $"Required {uniqueFoodTokenIds.Count} food visuals, " +
                    $"but only {availableIcons.Count} are available.",
                    this);

                return false;
            }

            Shuffle(availableIcons, new System.Random(seed));

            for (int i = 0; i < uniqueFoodTokenIds.Count; i++)
            {
                _iconByFoodTokenId.Add(uniqueFoodTokenIds[i], availableIcons[i]);
            }

            return true;
        }

        public Sprite ResolveIcon(int foodTokenId)
        {
            if (!_iconByFoodTokenId.TryGetValue(foodTokenId, out Sprite icon))
            {
                Debug.LogWarning($"Food visual is missing for token id {foodTokenId}.", this);

                return null;
            }

            return icon;
        }

        public void ClearMapping()
        {
            _iconByFoodTokenId.Clear();
        }

        private static List<int> GetUniqueFoodTokenIds(IReadOnlyList<int> foodTokenIds)
        {
            HashSet<int> uniqueTokenIds = new();
            List<int> result = new();

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (foodTokenId > 0 && uniqueTokenIds.Add(foodTokenId))
                {
                    result.Add(foodTokenId);
                }
            }

            result.Sort();
            return result;
        }

        private List<Sprite> GetAvailableIcons()
        {
            HashSet<Sprite> uniqueIcons = new();
            List<Sprite> result = new();

            IReadOnlyList<Sprite> icons = _visualCatalog.Icons;

            if (icons == null)
            {
                return result;
            }

            for (int i = 0; i < icons.Count; i++)
            {
                Sprite icon = icons[i];

                if (icon == null || !uniqueIcons.Add(icon))
                {
                    continue;
                }

                result.Add(icon);
            }

            return result;
        }

        private static void Shuffle(List<Sprite> icons, System.Random random)
        {
            for (int i = icons.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);

                Sprite temporary = icons[i];
                icons[i] = icons[randomIndex];
                icons[randomIndex] = temporary;
            }
        }
    }
}
