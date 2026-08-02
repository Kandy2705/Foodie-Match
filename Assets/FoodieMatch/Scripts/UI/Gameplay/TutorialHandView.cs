using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class TutorialHandView : MonoBehaviour
    {
        [SerializeField] private RectTransform _handMotionRoot;
        [SerializeField] private float _moveDuration = 0.25f;
        [SerializeField] private Ease _moveEase = Ease.OutCubic;

        private Tween _moveTween;
        private bool _didMoveComplete;

        private void OnDisable()
        {
            StopMotion();
        }

        public void ShowAt(Vector2 screenPosition)
        {
            StopMotion();
            _handMotionRoot.anchoredPosition = GetLocalPosition(screenPosition);
            gameObject.SetActive(true);
        }

        public async Task<MotionResult> MoveToAsync(Vector2 screenPosition)
        {
            StopMotion();
            _didMoveComplete = false;
            Vector2 targetPosition = GetLocalPosition(screenPosition);

            _moveTween = Tween.UIAnchoredPosition(
                    _handMotionRoot,
                    targetPosition,
                    _moveDuration,
                    _moveEase)
                .OnComplete(this, view => view._didMoveComplete = true);

            await _moveTween;

            return _didMoveComplete
                ? MotionResult.Completed
                : MotionResult.Cancelled;
        }

        public void Hide()
        {
            StopMotion();
            gameObject.SetActive(false);
        }

        private Vector2 GetLocalPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform,
                screenPosition,
                null,
                out Vector2 localPosition);
            return localPosition;
        }

        private void StopMotion()
        {
            if (_moveTween.isAlive)
            {
                _moveTween.Stop();
            }

            _moveTween = default;
        }
    }
}
