using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class FridgeBoosterAnchors : MonoBehaviour
    {
        [SerializeField]
        private FridgeBoosterView _fridgeBoosterView;

        public FridgeBoosterView FridgeBoosterView =>
            _fridgeBoosterView;

        private void Awake()
        {
            if (_fridgeBoosterView == null)
            {
                Debug.LogError(
                    "FridgeBoosterAnchors: " +
                    "FridgeBoosterView is missing.",
                    this);
            }
        }
    }
}