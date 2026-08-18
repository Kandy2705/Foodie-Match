using System;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.UI.Reward
{
    public sealed class SpoonRewardView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Appearance")]
        [SerializeField] private float _appearanceDuration = 0.35f;
        [SerializeField] private Ease _appearanceEase = Ease.OutBounce;
        [SerializeField] private Ease _fadeInEase = Ease.OutCubic;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _riseDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float _fallDuration = 0.3f;
        [SerializeField] private float _arcHeight = 240f;

        private RectTransform _rectTransform;
        private RectTransform _target;
        private Action<SpoonRewardView> _arrived;
        private Sequence _motionSequence;
        private Vector3 _visibleScale;
        private Vector3 _spawnPosition;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _visibleScale = _rectTransform.localScale;
        }

        public void Play(
            Vector3 spawnPosition,
            float startDelay,
            RectTransform target,
            Action<SpoonRewardView> arrived)
        {
            StopAndHide();
            _spawnPosition = spawnPosition;
            _target = target;
            _arrived = arrived;
            _rectTransform.localPosition = spawnPosition;
            _rectTransform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
            _rectTransform.SetAsLastSibling();

            float flightStartTime = startDelay + _appearanceDuration;
            float flightDuration = _riseDuration + _fallDuration;
            float arrivalTime = flightStartTime + flightDuration;

            _motionSequence = Sequence.Create(useUnscaledTime: true)
                .Insert(startDelay, Tween.Scale(
                    _rectTransform,
                    _visibleScale,
                    _appearanceDuration,
                    _appearanceEase))
                .Insert(startDelay, Tween.Alpha(
                    _canvasGroup,
                    1f,
                    _appearanceDuration,
                    _fadeInEase))
                .Insert(flightStartTime, Tween.Custom(
                    this,
                    0f,
                    1f,
                    flightDuration,
                    (spoon, progress) => spoon.UpdateFlightPosition(progress),
                    Ease.Linear))
                .InsertCallback(
                    arrivalTime,
                    this,
                    spoon => spoon.NotifyArrival());
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
            _motionSequence = default;
            _target = null;
            _arrived = null;
            gameObject.SetActive(false);
        }

        private void UpdateFlightPosition(float progress)
        {
            Vector3 targetPosition = GetTargetLocalPosition();
            Vector3 position = Vector3.LerpUnclamped(
                _spawnPosition,
                targetPosition,
                progress);
            position.y = CalculateFlightPositionY(
                progress,
                targetPosition.y);
            _rectTransform.localPosition = position;
        }

        private float CalculateFlightPositionY(
            float progress,
            float targetPositionY)
        {
            float peakPositionY = Mathf.Max(
                _spawnPosition.y,
                targetPositionY) + _arcHeight;
            float peakProgress = _riseDuration /
                                 (_riseDuration + _fallDuration);

            if (progress <= peakProgress)
            {
                float risingProgress = progress /
                                       peakProgress;
                float easedProgress = 1f -
                    (1f - risingProgress) *
                    (1f - risingProgress);
                return Mathf.LerpUnclamped(
                    _spawnPosition.y,
                    peakPositionY,
                    easedProgress);
            }

            float fallingProgress =
                (progress - peakProgress) /
                (1f - peakProgress);
            return Mathf.LerpUnclamped(
                peakPositionY,
                targetPositionY,
                fallingProgress * fallingProgress);
        }

        private Vector3 GetTargetLocalPosition()
        {
            return ((RectTransform)_rectTransform.parent)
                .InverseTransformPoint(_target.position);
        }

        private void NotifyArrival()
        {
            Action<SpoonRewardView> arrived = _arrived;
            Hide();
            arrived(this);
        }
    }
}
