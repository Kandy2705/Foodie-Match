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
        private static bool _didWarnAboutMissingFlyingSortingLayer;

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
        [SerializeField] private Ease _flightTransformEase = Ease.OutCubic;
        [SerializeField] private float _topTrayToGrillFlightDuration = 0.32f;
        [SerializeField] private string _flyingSortingLayerName = "FlyingFood";

        [Header("Landing Motion")]
        [SerializeField] private Vector3 _landingSquashScaleMultiplier = new(1.18f, 0.72f, 1f);
        [SerializeField] private float _landingSquashDuration = 0.08f;
        [SerializeField] private Ease _landingSquashEase = Ease.OutCubic;
        [SerializeField] private float _landingRestoreDuration = 0.1f;
        [SerializeField] private Ease _landingRestoreEase = Ease.OutBack;

        private Tween _flightTween;
        private Tween _fadeTween;
        private Sequence _landingSequence;
        private bool _isFlying;
        private bool _didFlightComplete;
        private bool _didFadeComplete;
        private bool _isLandingFeedbackPlaying;
        private bool _didLandingFeedbackComplete;
        private Transform _flightTarget;
        private Transform _landingTarget;
        private Vector3 _flightStartPosition;
        private Vector3 _latestFlightTargetPosition;
        private Vector3 _latestLandingTargetPosition;
        private Vector3 _scaleBeforeLanding;
        private Vector3 _flightStartScale;
        private Quaternion _flightStartRotation;
        private FoodItemVisualState? _flightTargetVisualState;
        private int _flyingSortingLayerId;
        private int _sortingLayerBeforeFlightId;
        private bool _hasFlyingSortingLayer;
        private bool _hasSortingLayerBeforeFlight;

        public int FoodTokenId { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsInteractable { get; private set; }
        public bool IsFlying => _isFlying;
        public float TopTrayToGrillFlightDuration => _topTrayToGrillFlightDuration;
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

            FindFlyingSortingLayer();
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
                SetSpriteAlpha(1f);
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
            return PlayFlightAsync(targetPosition, null, null, _flightDuration, startDelay);
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

            return PlayFlightAsync(target.position, target, null, _flightDuration, startDelay);
        }

        public Task<MotionResult> PlayFlightToGrillAsync(
            Vector3 targetPosition,
            float startDelay = 0f)
        {
            return PlayFlightAsync(
                targetPosition,
                null,
                FoodItemVisualState.OnGrill,
                _topTrayToGrillFlightDuration,
                startDelay);
        }

        private async Task<MotionResult> PlayFlightAsync(
            Vector3 targetPosition,
            Transform target,
            FoodItemVisualState? targetVisualState,
            float duration,
            float startDelay)
        {
            StopLandingFeedback(resetScale: true);

            if (!CanStartFlight(duration, startDelay))
            {
                return MotionResult.Failed;
            }

            _flightTarget = target;
            _flightStartPosition = transform.position;
            _latestFlightTargetPosition = targetPosition;
            _flightStartScale = transform.localScale;
            _flightStartRotation = transform.localRotation;
            _flightTargetVisualState = targetVisualState;
            _isFlying = true;
            _didFlightComplete = false;
            UseFlyingSortingLayer();
            SetInteractable(false);

            try
            {
                _flightTween = Tween.Custom(
                        this,
                        0f,
                        1f,
                        duration,
                        (foodItem, progress) => foodItem.UpdateFlightPosition(progress),
                        startDelay: startDelay)
                    .OnComplete(
                        target: this,
                        target => target.MarkFlightCompleted());

                await _flightTween;

                if (_didFlightComplete && _flightTargetVisualState.HasValue)
                {
                    SetVisualState(_flightTargetVisualState.Value);
                }

                return _didFlightComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (!_didFlightComplete)
                {
                    RestoreSortingLayerBeforeFlight();
                }

                _flightTween = default;
                _flightTarget = null;
                _flightTargetVisualState = null;
                _isFlying = false;
            }
        }

        public async Task<MotionResult> PlayFadeInAsync(float duration)
        {
            if (IsEmpty ||
                _spriteRenderer == null ||
                _fadeTween.isAlive ||
                !IsValidTime(duration))
            {
                return MotionResult.Failed;
            }

            SetSpriteAlpha(0f);
            _didFadeComplete = false;

            try
            {
                _fadeTween = Tween.Alpha(_spriteRenderer, 1f, duration)
                    .OnComplete(this, target => target.MarkFadeCompleted());

                await _fadeTween;

                return _didFadeComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                _fadeTween = default;
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
            StopFade(resetAlpha: true);
            RestoreSortingLayerBeforeFlight();
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
            RestoreSortingLayerBeforeFlight();
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

        private bool CanStartFlight(float duration, float startDelay)
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

            if (!IsValidTime(duration) ||
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

        private void MarkFadeCompleted()
        {
            _didFadeComplete = true;
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

            if (_flightTargetVisualState.HasValue)
            {
                UpdateFlightTransform(progress, _flightTargetVisualState.Value);
            }
        }

        private void UpdateFlightTransform(float progress, FoodItemVisualState targetVisualState)
        {
            float easedProgress = Easing.Evaluate(progress, _flightTransformEase);
            Vector3 targetScale = GetVisualScale(targetVisualState);
            Quaternion targetRotation = GetVisualRotation(targetVisualState);

            transform.localScale = Vector3.LerpUnclamped(_flightStartScale, targetScale, easedProgress);
            transform.localRotation = Quaternion.SlerpUnclamped(
                _flightStartRotation,
                targetRotation,
                easedProgress);
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

        private void StopFade(bool resetAlpha)
        {
            if (_fadeTween.isAlive)
            {
                _fadeTween.Stop();
            }

            _fadeTween = default;

            if (resetAlpha && _spriteRenderer != null)
            {
                SetSpriteAlpha(1f);
            }
        }

        private void SetSpriteAlpha(float alpha)
        {
            Color color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }

        private void FindFlyingSortingLayer()
        {
            if (string.IsNullOrWhiteSpace(_flyingSortingLayerName))
            {
                return;
            }

            _flyingSortingLayerId = SortingLayer.NameToID(_flyingSortingLayerName);
            _hasFlyingSortingLayer = SortingLayer.IDToName(_flyingSortingLayerId) == _flyingSortingLayerName;

            if (!_hasFlyingSortingLayer && !_didWarnAboutMissingFlyingSortingLayer)
            {
                Debug.LogWarning($"Sorting layer '{_flyingSortingLayerName}' is missing.", this);
                _didWarnAboutMissingFlyingSortingLayer = true;
            }
        }

        private void UseFlyingSortingLayer()
        {
            if (_spriteRenderer == null || !_hasFlyingSortingLayer)
            {
                return;
            }

            if (!_hasSortingLayerBeforeFlight)
            {
                _sortingLayerBeforeFlightId = _spriteRenderer.sortingLayerID;
                _hasSortingLayerBeforeFlight = true;
            }

            _spriteRenderer.sortingLayerID = _flyingSortingLayerId;
        }

        private void RestoreSortingLayerBeforeFlight()
        {
            if (_spriteRenderer != null && _hasSortingLayerBeforeFlight)
            {
                _spriteRenderer.sortingLayerID = _sortingLayerBeforeFlightId;
            }

            _hasSortingLayerBeforeFlight = false;
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

            transform.localScale = GetVisualScale(VisualState);
            transform.localRotation = GetVisualRotation(VisualState);
        }

        private Vector3 GetVisualScale(FoodItemVisualState visualState)
        {
            return visualState switch
            {
                FoodItemVisualState.OnTray => _trayScale,
                FoodItemVisualState.OnWaitingRack => _waitingRackScale,
                _ => _grillScale
            };
        }

        private Quaternion GetVisualRotation(FoodItemVisualState visualState)
        {
            return visualState switch
            {
                FoodItemVisualState.OnTray => Quaternion.Euler(_trayRotation),
                FoodItemVisualState.OnWaitingRack => Quaternion.Euler(_waitingRackRotation),
                _ => Quaternion.identity
            };
        }
    }
}
