using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageSlotView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _previewAlpha = 0.45f;
        [SerializeField] private float _filledAlpha = 1f;
        [SerializeField] private int _filledSortingOrderOffset = 10;

        private int _previewSortingOrder;

        public bool IsVisible { get; private set; }
        public bool IsFilled { get; private set; }

        private void Awake()
        {
            _previewSortingOrder = _spriteRenderer.sortingOrder;
            ApplyVisibility();
        }

        public void Show(Sprite sprite, bool isFilled)
        {
            IsVisible = true;
            IsFilled = isFilled;
            _spriteRenderer.sprite = sprite;
            ApplyVisibility();
        }

        public void Hide()
        {
            IsVisible = false;
            IsFilled = false;
            _spriteRenderer.sprite = null;
            ApplyVisibility();
        }

        public void SetFilled()
        {
            if (!IsVisible)
            {
                return;
            }

            IsFilled = true;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            _spriteRenderer.enabled = IsVisible && _spriteRenderer.sprite != null;
            _spriteRenderer.color = GetCurrentColor();
            _spriteRenderer.sortingOrder = IsFilled
                ? _previewSortingOrder + _filledSortingOrderOffset
                : _previewSortingOrder;
        }

        private Color GetCurrentColor()
        {
            Color color = _spriteRenderer.color;
            color.a = IsFilled ? _filledAlpha : _previewAlpha;
            return color;
        }
    }
}
