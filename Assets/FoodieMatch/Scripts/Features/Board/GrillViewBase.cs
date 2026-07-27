using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public abstract class GrillViewBase : MonoBehaviour
    {
        public abstract int FoodAnchorCount { get; }

        public abstract Transform GetFoodAnchor(int index);
    }
}
