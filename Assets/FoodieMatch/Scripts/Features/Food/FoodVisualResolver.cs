using System.Collections.Generic;
using FoodieMatch.Data.Food;
using UnityEngine;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodVisualResolver : MonoBehaviour
    {
        [SerializeField] private FoodVisualDataSO[] _visuals;

        private readonly Dictionary<int, FoodVisualDataSO>
            _visualByFoodTokenId = new();

        public bool TryCreateRandomMapping(
            IReadOnlyList<int> foodTokenIds)
        {
            ClearMapping();

            if (foodTokenIds == null || foodTokenIds.Count == 0)
            {
                Debug.LogError(
                    "Food token collection is empty.",
                    this);

                return false;
            }

            List<int> uniqueFoodTokenIds =
                GetUniqueFoodTokenIds(foodTokenIds);

            List<FoodVisualDataSO> availableVisuals =
                GetAvailableVisuals();

            if (availableVisuals.Count < uniqueFoodTokenIds.Count)
            {
                Debug.LogError(
                    $"Required {uniqueFoodTokenIds.Count} food visuals, " +
                    $"but only {availableVisuals.Count} are available.",
                    this);

                return false;
            }

            Shuffle(availableVisuals);

            for (int i = 0;
                 i < uniqueFoodTokenIds.Count;
                 i++)
            {
                _visualByFoodTokenId.Add(
                    uniqueFoodTokenIds[i],
                    availableVisuals[i]);
            }

            return true;
        }

        public Sprite ResolveIcon(int foodTokenId)
        {
            if (!_visualByFoodTokenId.TryGetValue(
                    foodTokenId,
                    out FoodVisualDataSO visual))
            {
                Debug.LogWarning(
                    $"Food visual is missing for token id {foodTokenId}.",
                    this);

                return null;
            }

            return visual.Icon;
        }

        public void ClearMapping()
        {
            _visualByFoodTokenId.Clear();
        }

        private static List<int> GetUniqueFoodTokenIds(
            IReadOnlyList<int> foodTokenIds)
        {
            HashSet<int> uniqueTokenIds = new HashSet<int>();
            List<int> result = new List<int>();

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (foodTokenId > 0 &&
                    uniqueTokenIds.Add(foodTokenId))
                {
                    result.Add(foodTokenId);
                }
            }

            result.Sort();
            return result;
        }

        private List<FoodVisualDataSO> GetAvailableVisuals()
        {
            HashSet<Sprite> uniqueIcons = new HashSet<Sprite>();
            List<FoodVisualDataSO> result =
                new List<FoodVisualDataSO>();

            if (_visuals == null)
            {
                return result;
            }

            for (int i = 0; i < _visuals.Length; i++)
            {
                FoodVisualDataSO visual = _visuals[i];

                if (visual == null ||
                    visual.Icon == null ||
                    !uniqueIcons.Add(visual.Icon))
                {
                    continue;
                }

                result.Add(visual);
            }

            return result;
        }

        private static void Shuffle(
            List<FoodVisualDataSO> visuals)
        {
            for (int i = visuals.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                FoodVisualDataSO temporary = visuals[i];
                visuals[i] = visuals[randomIndex];
                visuals[randomIndex] = temporary;
            }
        }
    }
}
