using System.Collections.Generic;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class GrillView : MonoBehaviour
    {
        [SerializeField] private FoodItemView[] _foodSlots;

        public void Setup(IReadOnlyList<int> foodTokenIds, FoodVisualResolver foodVisualResolver)
        {
            if (_foodSlots == null)
            {
                return;
            }

            for (var i = 0; i < _foodSlots.Length; i++)
            {
                if (_foodSlots[i] == null)
                {
                    continue;
                }

                var foodTokenId = foodTokenIds != null && i < foodTokenIds.Count
                    ? foodTokenIds[i]
                    : 0;

                var sprite = foodVisualResolver != null
                    ? foodVisualResolver.ResolveIcon(foodTokenId)
                    : null;

                _foodSlots[i].Setup(foodTokenId, sprite);
                _foodSlots[i].SetVisualState(FoodItemVisualState.OnGrill);
                _foodSlots[i].SetInteractable(false);
            }
        }
    }
}
