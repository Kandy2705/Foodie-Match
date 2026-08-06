using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FoodieMatch.UI.Reward
{
    public sealed class CoinRewardView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Appearance")]
        [SerializeField] private float _scaleUpDuration = 0.12f;
        [SerializeField] private float _scaleSettleDuration = 0.08f;
        [SerializeField] private float _overshootScale = 1.2f;
        [SerializeField] private Ease _scaleUpEase = Ease.OutCubic;
        [SerializeField] private Ease _scaleSettleEase = Ease.InOutSine;
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private Ease _fadeInEase = Ease.OutCubic;
        [SerializeField] private float _arrivalHoldDuration = 0.1f;

        [Header("Movement")]
        [SerializeField] private float _retreatStrength = 150f;
        [SerializeField] private Vector2 _movementDurationRange = new(0.75f, 1f);
        [SerializeField] private float _curveOffset = 60f;
        [SerializeField] private Ease _movementEase = Ease.Linear;

        private RectTransform _rectTransform;
        private RectTransform _target;
        private Action<CoinRewardView> _arrived;
        private Action<CoinRewardView> _arrivalHoldCompleted;
        private Sequence _motionSequence;
        private Vector3 _visibleScale;
        private Vector3 _spawnPosition;
        private float _curveDirection;

        public float AppearanceDuration => Mathf.Max(
            _scaleUpDuration + _scaleSettleDuration,
            _fadeInDuration);
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _visibleScale = _rectTransform.localScale;
        }

        public void Play(
            Vector3 spawnPosition,
            float appearanceStartTime,
            float movementStartTime,
            RectTransform target,
            Action<CoinRewardView> arrived,
            Action<CoinRewardView> arrivalHoldCompleted)
        {
            StopAndHide();
            _spawnPosition = spawnPosition;
            _target = target;
            _arrived = arrived;
            _arrivalHoldCompleted = arrivalHoldCompleted;
            _curveDirection = Random.value < 0.5f ? -1f : 1f;
            _rectTransform.localPosition = spawnPosition;
            _rectTransform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
            _rectTransform.SetAsLastSibling();
            IsPlaying = true;

            float movementDuration = GetRandomMovementDuration();
            float arrivalTime = movementStartTime + movementDuration;
            float scaleSettleStartTime = appearanceStartTime + _scaleUpDuration;

            _motionSequence = Sequence.Create(useUnscaledTime: true)
                .Insert(appearanceStartTime, Tween.Scale(
                    _rectTransform,
                    _visibleScale * _overshootScale,
                    _scaleUpDuration,
                    _scaleUpEase))
                .Insert(scaleSettleStartTime, Tween.Scale(
                    _rectTransform,
                    _visibleScale,
                    _scaleSettleDuration,
                    _scaleSettleEase))
                .Insert(appearanceStartTime, Tween.Alpha(
                    _canvasGroup,
                    1f,
                    _fadeInDuration,
                    _fadeInEase))
                .Insert(movementStartTime, Tween.Custom(
                    this,
                    0f,
                    1f,
                    movementDuration,
                    (coin, progress) => coin.UpdatePosition(progress),
                    _movementEase))
                .InsertCallback(arrivalTime, this, coin => coin.NotifyArrival())
                .InsertCallback(
                    arrivalTime + _arrivalHoldDuration,
                    this,
                    coin => coin.CompleteArrivalHold());
        }

        public void StopAndHide()
        {
            if (_motionSequence.isAlive)
            {
                _motionSequence.Stop();
            }

            Hide();
        }

        private void Hide()
        {
            IsPlaying = false;
            _motionSequence = default;
            _target = null;
            _arrived = null;
            _arrivalHoldCompleted = null;
            gameObject.SetActive(false);
        }

        private float GetRandomMovementDuration()
        {
            float minimum = Mathf.Max(0.01f, Mathf.Min(
                _movementDurationRange.x,
                _movementDurationRange.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                _movementDurationRange.x,
                _movementDurationRange.y));
            return Random.Range(minimum, maximum);
        }

        private void UpdatePosition(float progress)
        {
            Vector3 targetPosition = GetTargetLocalPosition();
            Vector3 targetDirection = targetPosition - _spawnPosition;
            Vector3 retreatDirection = targetDirection.sqrMagnitude <= Mathf.Epsilon
                ? Vector3.down
                : -targetDirection.normalized;
            Vector3 curveDirection = new(-targetDirection.y, targetDirection.x, 0f);

            if (curveDirection.sqrMagnitude > Mathf.Epsilon)
            {
                curveDirection.Normalize();
            }

            Vector3 retreatControlPoint = _spawnPosition + retreatDirection * _retreatStrength;
            Vector3 curveControlPoint = Vector3.Lerp(_spawnPosition, targetPosition, 0.5f) +
                                        curveDirection * _curveOffset * _curveDirection;
            float remainingProgress = 1f - progress;
            _rectTransform.localPosition =
                remainingProgress * remainingProgress * remainingProgress * _spawnPosition +
                3f * remainingProgress * remainingProgress * progress * retreatControlPoint +
                3f * remainingProgress * progress * progress * curveControlPoint +
                progress * progress * progress * targetPosition;
        }

        private Vector3 GetTargetLocalPosition()
        {
            return ((RectTransform)_rectTransform.parent).InverseTransformPoint(_target.position);
        }

        private void NotifyArrival()
        {
            _arrived(this);
        }

        private void CompleteArrivalHold()
        {
            Action<CoinRewardView> arrivalHoldCompleted = _arrivalHoldCompleted;
            Hide();
            arrivalHoldCompleted(this);
        }
    }
}
