using System.Collections.Generic;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public readonly struct TopTrayMoveVisuals
    {
        public TopTrayMoveVisuals(
            IReadOnlyList<FoodItemView> movingFoodItems,
            IReadOnlyList<Transform> targetAnchors,
            TrayView departingTray,
            IReadOnlyList<FoodItemView> newTopTrayFoodItems)
        {
            MovingFoodItems = movingFoodItems;
            TargetAnchors = targetAnchors;
            DepartingTray = departingTray;
            NewTopTrayFoodItems = newTopTrayFoodItems;
        }

        public IReadOnlyList<FoodItemView> MovingFoodItems { get; }
        public IReadOnlyList<Transform> TargetAnchors { get; }
        public TrayView DepartingTray { get; }
        public IReadOnlyList<FoodItemView> NewTopTrayFoodItems { get; }
    }
}
