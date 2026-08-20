using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class GameplayInputBlockRegion : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public bool Contains(Vector2 screenPosition)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                screenPosition);
        }
    }
}
