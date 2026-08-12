using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public abstract class GrillViewBase : MonoBehaviour
    {
        [Header("Intro Motion")]
        [SerializeField] private float _introHorizontalOffset = 10f;
        [SerializeField] private float _introDuration = 0.45f;
        [SerializeField] private Ease _introEase = Ease.OutBack;

        private Tween _introTween;
        private Vector3 _introTargetPosition;
        private bool _hasIntroTargetPosition;
        private bool _didIntroFinish;

        public abstract int FoodAnchorCount { get; }

        protected virtual void OnDestroy()
        {
            StopIntroMotion();
        }

        public void PrepareIntro()
        {
            StopIntroMotion();
            _introTargetPosition = transform.localPosition;
            _hasIntroTargetPosition = true;
            float direction = _introTargetPosition.x > 0f ? 1f : -1f;
            transform.localPosition = _introTargetPosition +
                                      Vector3.right * Mathf.Abs(_introHorizontalOffset) * direction;
            _didIntroFinish = false;
        }

        public async Task<MotionResult> PlayIntroAsync(float startDelay)
        {
            if (!_hasIntroTargetPosition)
            {
                return MotionResult.Failed;
            }

            try
            {
                _introTween = Tween.LocalPosition(
                        transform,
                        _introTargetPosition,
                        _introDuration,
                        _introEase,
                        startDelay: startDelay)
                    .OnComplete(this, target => target.MarkIntroFinished());
                await _introTween;

                return _didIntroFinish
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                transform.localPosition = _introTargetPosition;
                _hasIntroTargetPosition = false;
                _introTween = default;
            }
        }

        public void StopIntroMotion()
        {
            if (_introTween.isAlive)
            {
                _introTween.Stop();
            }

            if (_hasIntroTargetPosition)
            {
                transform.localPosition = _introTargetPosition;
                _hasIntroTargetPosition = false;
            }

            _introTween = default;
        }

        public abstract Transform GetFoodAnchor(int index);

        public abstract void ResetForUse();

        public abstract void ResetForPool();

        private void MarkIntroFinished()
        {
            _didIntroFinish = true;
        }
    }
}
