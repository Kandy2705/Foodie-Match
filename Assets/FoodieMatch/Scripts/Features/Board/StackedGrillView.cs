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

        public override int FoodAnchorCount => _foodAnchors.Length;

        public override Transform GetFoodAnchor(int index)
        {
            return index >= 0 && index < _foodAnchors.Length
                ? _foodAnchors[index]
                : null;
        }

        public Tween PlayExitMotion()
        {
            return Tween.Scale(
                transform,
                Vector3.zero,
                _exitDuration,
                _exitEase);
        }

        public Tween PlaySlideMotion(Vector3 targetLocalPosition)
        {
            return Tween.LocalPosition(
                transform,
                targetLocalPosition,
                _slideDuration,
                _slideEase);
        }
    }
}
