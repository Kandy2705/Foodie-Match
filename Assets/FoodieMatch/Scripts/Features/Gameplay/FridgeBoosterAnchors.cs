using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class FridgeBoosterAnchors : MonoBehaviour
    {
        [SerializeField]
        private FridgeBoosterView _fridgeBoosterView;

        public FridgeBoosterView FridgeBoosterView =>
            _fridgeBoosterView;
    }
}
