using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class StackedGrillView : GrillViewBase
    {
        [SerializeField] private Transform[] _foodAnchors;

        [Header("Exit Motion")]
        [SerializeField, Min(0.01f)] private float _exitDuration = 0.18f;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        [Header("Slide Motion")]
        [SerializeField, Min(0.01f)] private float _slideDuration = 0.22f;
        [SerializeField] private Ease _slideEase = Ease.OutCubic;

        private Tween _exitTween;
        private Tween _slideTween;
        private Vector3 _initialScale;

        public override int FoodAnchorCount => _foodAnchors.Length;

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        private void OnDestroy()
        {
            CancelMotion();
        }

        public override Transform GetFoodAnchor(int index)
        {
            return index >= 0 && index < _foodAnchors.Length
                ? _foodAnchors[index]
                : null;
        }

        public Tween PlayExitMotion()
        {
            _exitTween = Tween.Scale(
                transform,
                Vector3.zero,
                _exitDuration,
                _exitEase);
            return _exitTween;
        }

        public Tween PlaySlideMotion(Vector3 targetLocalPosition)
        {
            _slideTween = Tween.LocalPosition(
                transform,
                targetLocalPosition,
                _slideDuration,
                _slideEase);
            return _slideTween;
        }

        public override void ResetForUse()
        {
            CancelMotion();
        }

        public override void ResetForPool()
        {
            CancelMotion();
        }

        private void CancelMotion()
        {
            if (_exitTween.isAlive)
            {
                _exitTween.Stop();
            }

            if (_slideTween.isAlive)
            {
                _slideTween.Stop();
            }

            _exitTween = default;
            _slideTween = default;
            transform.localScale = _initialScale;
        }
    }
}
