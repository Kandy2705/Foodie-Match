using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay.Booster
{
    public sealed class BoosterUnlockRewardView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float _moveDelay = 0.2f;
        [SerializeField, Min(0f)] private float _moveDuration = 0.6f;
        [SerializeField, Range(0f, 0.9f)] private float _backwardMotionProgress = 0.4f;
        [SerializeField] private Ease _moveEase = Ease.InBack;
        [SerializeField] private Ease _scaleEase = Ease.InCubic;
        [SerializeField] private Ease _fadeEase = Ease.InCubic;

        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        private Vector3 _visibleScale;
        private Sequence _motionSequence;
        private bool _didMotionComplete;

        public void Initialize()
        {
            _rectTransform = (RectTransform)transform;
            _startPosition = _rectTransform.anchoredPosition;
            _visibleScale = _rectTransform.localScale;
            Hide();
        }

        public async Task<MotionResult> PlayAsync(
            Sprite icon,
            int amount,
            RectTransform target)
        {
            StopMotion();

            _iconImage.sprite = icon;
            _iconImage.SetNativeSize();
            _amountText.text = $"x{amount}";
            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.localScale = _visibleScale;
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            _didMotionComplete = false;

            float visualTransitionDelay =
                _moveDelay + _moveDuration * _backwardMotionProgress;
            float visualTransitionDuration =
                _moveDuration * (1f - _backwardMotionProgress);
            float completionTime = _moveDelay + _moveDuration;
            _motionSequence = Sequence.Create(useUnscaledTime: true)
                .Insert(_moveDelay, Tween.Position(
                    _rectTransform,
                    target.position,
                    _moveDuration,
                    _moveEase))
                .Insert(visualTransitionDelay, Tween.Scale(
                    _rectTransform,
                    Vector3.zero,
                    visualTransitionDuration,
                    _scaleEase))
                .Insert(visualTransitionDelay, Tween.Alpha(
                    _canvasGroup,
                    0f,
                    visualTransitionDuration,
                    _fadeEase))
                .InsertCallback(
                    completionTime,
                    this,
                    view => view.MarkMotionCompleted());

            await _motionSequence;

            MotionResult result = _didMotionComplete
                ? MotionResult.Completed
                : MotionResult.Cancelled;
            Hide();
            return result;
        }

        public void StopAndHide()
        {
            StopMotion();
            Hide();
        }

        private void MarkMotionCompleted()
        {
            _didMotionComplete = true;
        }

        private void StopMotion()
        {
            if (_motionSequence.isAlive)
            {
                _motionSequence.Stop();
            }

            _motionSequence = default;
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
