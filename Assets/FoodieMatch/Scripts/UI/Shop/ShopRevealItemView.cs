using PrimeTween;
using UnityEngine;

namespace FoodieMatch.UI.Shop
{
    public sealed class ShopRevealItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform _motionRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        private ShopProductCardView _productCard;
        private Vector2 _visiblePosition;
        private Vector3 _visibleScale;
        private float _visibleAlpha;
        private bool _isInitialized;

        public void Initialize(ShopProductCardView productCard)
        {
            if (_isInitialized)
            {
                return;
            }

            _productCard = productCard;
            _visiblePosition = _motionRoot.anchoredPosition;
            _visibleScale = _motionRoot.localScale;
            _visibleAlpha = _canvasGroup.alpha;
            _isInitialized = true;
        }

        public void Prepare(Vector2 startOffset, float startScaleMultiplier)
        {
            _motionRoot.anchoredPosition = _visiblePosition + startOffset;
            _motionRoot.localScale = _visibleScale * startScaleMultiplier;
            _canvasGroup.alpha = 0f;
            _productCard?.SetRevealComplete(false);
        }

        public Sequence InsertReveal(
            Sequence sequence,
            float startTime,
            float duration,
            Ease moveEase,
            Ease scaleEase,
            Ease fadeEase)
        {
            return sequence
                .Insert(startTime, Tween.UIAnchoredPosition(
                    _motionRoot,
                    _visiblePosition,
                    duration,
                    moveEase))
                .Insert(startTime, Tween.Scale(
                    _motionRoot,
                    _visibleScale,
                    duration,
                    scaleEase))
                .Insert(startTime, Tween.Alpha(
                    _canvasGroup,
                    _visibleAlpha,
                    duration,
                    fadeEase))
                .InsertCallback(
                    startTime + duration,
                    this,
                    item => item.CompleteReveal());
        }

        public void Restore()
        {
            _motionRoot.anchoredPosition = _visiblePosition;
            _motionRoot.localScale = _visibleScale;
            _canvasGroup.alpha = _visibleAlpha;
            _productCard?.SetRevealComplete(true);
        }

        private void CompleteReveal()
        {
            Restore();
        }
    }
}
