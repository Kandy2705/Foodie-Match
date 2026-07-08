using FoodieMatch.Data.Food;
using UnityEngine;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodVisualResolver : MonoBehaviour
    {
        [SerializeField] private FoodVisualDataSO[] _visuals;

        public Sprite ResolveIcon(int foodTokenId)
        {
            if (foodTokenId <= 0)
            {
                return null;
            }

            var visualIndex = foodTokenId - 1;

            if (_visuals == null || visualIndex >= _visuals.Length || _visuals[visualIndex] == null)
            {
                Debug.LogWarning($"Food visual is missing for token id {foodTokenId}.", this);
                return null;
            }

            return _visuals[visualIndex].Icon;
        }
    }
}
