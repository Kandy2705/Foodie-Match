using System;
using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodItemView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _clickCollider;

        [Header("Grill")]
        [SerializeField] private Vector3 _grillScale = Vector3.one;

        [Header("Tray")]
        [SerializeField] private Vector3 _trayScale = new Vector3(0.75f, 0.75f, 1f);
        [SerializeField] private Vector3 _trayRotation;

        [Header("Waiting Rack")]
        [SerializeField] private Vector3 _waitingRackScale = Vector3.one;
        [SerializeField] private Vector3 _waitingRackRotation;

        [Header("Flight Motion")]
        [SerializeField] private float _flightDuration = 0.22f;
        [SerializeField] private float _flightArcHeight = 1f;
        [SerializeField, Range(0.1f, 0.9f)] private float _flightPeakProgress = 0.45f;

        [Header("Landing Motion")]
        [SerializeField] private Vector3 _landingSquashScaleMultiplier = new(1.18f, 0.72f, 1f);
        [SerializeField] private float _landingSquashDuration = 0.08f;
        [SerializeField] private Ease _landingSquashEase = Ease.OutCubic;
        [SerializeField] private float _landingRestoreDuration = 0.1f;
        [SerializeField] private Ease _landingRestoreEase = Ease.OutBack;

        private Tween _flightTween;
        private Sequence _landingSequence;
        private bool _isFlying;
        private bool _didFlightComplete;
        private bool _isLandingFeedbackPlaying;
        private bool _didLandingFeedbackComplete;
        private Transform _flightTarget;
        private Transform _landingTarget;
        private Vector3 _flightStartPosition;
        private Vector3 _latestFlightTargetPosition;
        private Vector3 _latestLandingTargetPosition;
        private Vector3 _scaleBeforeLanding;

        public int FoodTokenId { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsInteractable { get; private set; }
        public bool IsFlying => _isFlying;
        public FoodItemVisualState VisualState { get; private set; }

        public event Action<FoodItemView> Selected;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_clickCollider == null)
            {
                _clickCollider = GetComponent<Collider2D>();
            }

            ApplyColliderState();
            ApplyVisualState();
        }

        private void OnDestroy()
        {
            CancelMotion();
        }

        private void LateUpdate()
        {
            if (_isLandingFeedbackPlaying)
            {
                UpdateLandingPosition();
            }
        }

        public void Setup(int foodTokenId, Sprite sprite)
        {
            if (_isFlying)
            {
                Debug.LogError(
                    "Flying food item cannot be set up again.",
                    this);
                return;
            }

            CancelMotion();

            if (foodTokenId < 0)
            {
                Debug.LogWarning($"Food token id cannot be negative: {foodTokenId}.", this);
                Clear();
                return;
            }

            if (foodTokenId == 0)
            {
                Clear();
                return;
            }

            FoodTokenId = foodTokenId;
            VisualState = FoodItemVisualState.OnGrill;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.enabled = sprite != null;
            }

            ApplyColliderState();
            ApplyVisualState();
        }

        public void Clear()
        {
            CancelMotion();
            FoodTokenId = 0;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = null;
                _spriteRenderer.enabled = false;
            }

            ApplyColliderState();
            ApplyVisualState();
        }

        public Task<MotionResult> PlayFlightAsync(
            Vector3 targetPosition,
            float startDelay = 0f)
        {
            return PlayFlightAsync(targetPosition, null, startDelay);
        }

        public Task<MotionResult> PlayFlightAsync(
            Transform target,
            float startDelay = 0f)
        {
            if (target == null)
            {
                Debug.LogError("Food flight target is missing.", this);
                return Task.FromResult(MotionResult.Failed);
            }

            return PlayFlightAsync(target.position, target, startDelay);
        }

        private async Task<MotionResult> PlayFlightAsync(
            Vector3 targetPosition,
            Transform target,
            float startDelay)
        {
            StopLandingFeedback(resetScale: true);

            if (!CanStartFlight(startDelay))
            {
                return MotionResult.Failed;
            }

            _flightTarget = target;
            _flightStartPosition = transform.position;
            _latestFlightTargetPosition = targetPosition;
            _isFlying = true;
            _didFlightComplete = false;
            SetInteractable(false);

            try
            {
                _flightTween = Tween.Custom(
                        this,
                        0f,
                        1f,
                        _flightDuration,
                        (foodItem, progress) => foodItem.UpdateFlightPosition(progress),
                        startDelay: startDelay)
                    .OnComplete(
                        target: this,
                        target => target.MarkFlightCompleted());

                await _flightTween;

                return _didFlightComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                _flightTween = default;
                _flightTarget = null;
                _isFlying = false;
            }
        }

        public void StopFlight()
        {
            if (_flightTween.isAlive)
            {
                _flightTween.Stop();
            }
        }

        public void CancelMotion()
        {
            StopFlight();
            StopLandingFeedback(resetScale: true);
        }

        public async Task<MotionResult> PlayLandingFeedbackAsync(Transform target = null)
        {
            if (IsEmpty ||
                _isLandingFeedbackPlaying ||
                !IsValidScale(_landingSquashScaleMultiplier) ||
                !IsValidTime(_landingSquashDuration) ||
                !IsValidTime(_landingRestoreDuration))
            {
                return MotionResult.Failed;
            }

            if (_landingSquashDuration == 0f && _landingRestoreDuration == 0f)
            {
                return MotionResult.Completed;
            }

            _scaleBeforeLanding = transform.localScale;
            Vector3 squashScale = Vector3.Scale(_scaleBeforeLanding, _landingSquashScaleMultiplier);
            _landingTarget = target;

            if (_landingTarget != null)
            {
                _latestLandingTargetPosition = _landingTarget.position;
            }

            _isLandingFeedbackPlaying = true;
            _didLandingFeedbackComplete = false;

            try
            {
                _landingSequence = Sequence.Create()
                    .Chain(Tween.Scale(
                        transform, squashScale, _landingSquashDuration, _landingSquashEase))
                    .Chain(Tween.Scale(
                        transform, _scaleBeforeLanding, _landingRestoreDuration, _landingRestoreEase))
                    .ChainCallback(this, target => target.MarkLandingFeedbackCompleted());

                await _landingSequence;

                if (_didLandingFeedbackComplete)
                {
                    UpdateLandingPosition();
                }

                return _didLandingFeedbackComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                _landingSequence = default;
                _landingTarget = null;
                _isLandingFeedbackPlaying = false;
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            ApplyColliderState();
        }

        public void SetVisualState(FoodItemVisualState visualState)
        {
            VisualState = visualState;
            ApplyVisualState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsEmpty || !IsInteractable)
            {
                return;
            }

            Selected?.Invoke(this);
        }

        private bool CanStartFlight(float startDelay)
        {
            if (IsEmpty)
            {
                Debug.LogError(
                    "Empty food item cannot start a flight.",
                    this);
                return false;
            }

            if (_isFlying)
            {
                Debug.LogError(
                    "Food item is already flying.",
                    this);
                return false;
            }

            if (!IsValidTime(_flightDuration) ||
                !IsValidPositiveNumber(_flightArcHeight) ||
                !IsValidPeakProgress(_flightPeakProgress) ||
                !IsValidTime(startDelay))
            {
                Debug.LogError(
                    "Food flight time is invalid.",
                    this);
                return false;
            }

            return true;
        }

        private void MarkFlightCompleted()
        {
            _didFlightComplete = true;
        }

        private void MarkLandingFeedbackCompleted()
        {
            _didLandingFeedbackComplete = true;
        }

        private void UpdateFlightPosition(float progress)
        {
            if (_flightTarget != null)
            {
                _latestFlightTargetPosition = _flightTarget.position;
            }

            Vector3 position = Vector3.LerpUnclamped(
                _flightStartPosition,
                _latestFlightTargetPosition,
                progress);
            position.y = CalculateFlightPositionY(progress);
            transform.position = position;
        }

        private float CalculateFlightPositionY(float progress)
        {
            float targetPositionY = _latestFlightTargetPosition.y;
            float peakPositionY = Mathf.Max(_flightStartPosition.y, targetPositionY) + _flightArcHeight;

            if (progress <= _flightPeakProgress)
            {
                float risingProgress = progress / _flightPeakProgress;
                float easedProgress = 1f - (1f - risingProgress) * (1f - risingProgress);
                return Mathf.LerpUnclamped(_flightStartPosition.y, peakPositionY, easedProgress);
            }

            float fallingProgress = (progress - _flightPeakProgress) / (1f - _flightPeakProgress);
            return Mathf.LerpUnclamped(peakPositionY, targetPositionY, fallingProgress * fallingProgress);
        }

        private void UpdateLandingPosition()
        {
            if (_landingTarget != null)
            {
                _latestLandingTargetPosition = _landingTarget.position;
                transform.position = _latestLandingTargetPosition;
            }
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsValidPositiveNumber(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsValidPeakProgress(float value)
        {
            return value > 0f && value < 1f && !float.IsNaN(value);
        }

        private void StopLandingFeedback(bool resetScale)
        {
            if (_landingSequence.isAlive)
            {
                _landingSequence.Stop();
            }

            _landingSequence = default;
            _landingTarget = null;

            if (resetScale && _isLandingFeedbackPlaying)
            {
                transform.localScale = _scaleBeforeLanding;
            }
        }

        private static bool IsValidScale(Vector3 value)
        {
            return IsValidScaleValue(value.x) &&
                   IsValidScaleValue(value.y) &&
                   IsValidScaleValue(value.z);
        }

        private static bool IsValidScaleValue(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void ApplyColliderState()
        {
            if (_clickCollider != null)
            {
                _clickCollider.enabled = !IsEmpty && IsInteractable;
            }
        }

        private void ApplyVisualState()
        {
            if (IsEmpty)
            {
                VisualState = FoodItemVisualState.Empty;

                if (_spriteRenderer != null)
                {
                    _spriteRenderer.enabled = false;
                }

                return;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = _spriteRenderer.sprite != null;
            }

            if (VisualState == FoodItemVisualState.OnTray)
            {
                transform.localScale = _trayScale;
                transform.localEulerAngles = _trayRotation;
                return;
            }

            if (VisualState == FoodItemVisualState.OnWaitingRack)
            {
                transform.localScale = _waitingRackScale;
                transform.localEulerAngles = _waitingRackRotation;
                return;
            }

            transform.localScale = _grillScale;
            transform.localRotation = Quaternion.identity;
        }
    }
}
