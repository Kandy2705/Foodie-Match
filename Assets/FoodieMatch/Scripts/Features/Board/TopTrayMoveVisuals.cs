using System.Collections.Generic;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public readonly struct TopTrayMoveVisuals
    {
        public TopTrayMoveVisuals(
            IReadOnlyList<FoodItemView> movingFoodItems,
            IReadOnlyList<Vector3> targetPositions,
            TrayView departingTray,
            IReadOnlyList<FoodItemView> newTopTrayFoodItems)
        {
            MovingFoodItems = movingFoodItems;
            TargetPositions = targetPositions;
            DepartingTray = departingTray;
            NewTopTrayFoodItems = newTopTrayFoodItems;
        }

        public IReadOnlyList<FoodItemView> MovingFoodItems { get; }
        public IReadOnlyList<Vector3> TargetPositions { get; }
        public TrayView DepartingTray { get; }
        public IReadOnlyList<FoodItemView> NewTopTrayFoodItems { get; }
    }
}
