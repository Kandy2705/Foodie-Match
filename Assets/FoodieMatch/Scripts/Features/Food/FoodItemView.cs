using System;
using System.Threading.Tasks;
using FoodieMatch.Features.Effects;
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

        [Header("Grill Smoke")]
        [SerializeField] private Vector3 _grillSmokeOffset = new(0f, -0.1f, 0f);

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

        [Header("Reveal Motion")]
        [SerializeField] private float _revealDuration = 0.18f;
        [SerializeField] private Ease _revealEase = Ease.OutBack;

        private Tween _flightTween;
        private Tween _fadeTween;
        private Tween _revealTween;
        private Sequence _landingSequence;
        private Sequence _scaleSequence;
        private bool _isFlying;
        private bool _didFlightComplete;
        private bool _didFadeComplete;
        private bool _isRevealPlaying;
        private bool _didRevealComplete;
        private bool _isLandingFeedbackPlaying;
        private bool _didLandingFeedbackComplete;
        private bool _didScaleComplete;
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
        private int _reuseVersion;
        private ParticleEffectPool _grillSmokePool;

        public int FoodTokenId { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsInteractable { get; private set; }
        public bool IsFlying => _isFlying;
        public float TopTrayToGrillFlightDuration => _topTrayToGrillFlightDuration;
        public FoodItemVisualState VisualState { get; private set; }

        public event Action<FoodItemView> Selected;

        public void Construct(ParticleEffectPool grillSmokePool)
        {
            _grillSmokePool = grillSmokePool;
        }

        private void Awake()
        {
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

            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = true;
            SetSpriteAlpha(1f);

            ApplyColliderState();
            ApplyVisualState();
        }

        public void Clear()
        {
            ResetForPool();
        }

        public void ResetForUse()
        {
            ResetViewState();
        }

        public void ResetForPool()
        {
            ResetViewState();
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
            Transform target,
            float startDelay = 0f)
        {
            if (target == null)
            {
                Debug.LogError(
                    "Food grill flight target is missing.",
                    this);
                return Task.FromResult(
                    MotionResult.Failed);
            }

            return PlayFlightAsync(
                target.position,
                target,
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

            int reuseVersion = _reuseVersion;
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

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

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
                if (IsCurrentReuse(reuseVersion))
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
        }

        public async Task<MotionResult> PlayFadeInAsync(float duration)
        {
            if (IsEmpty ||
                _fadeTween.isAlive ||
                !IsValidTime(duration))
            {
                return MotionResult.Failed;
            }

            SetSpriteAlpha(0f);
            int reuseVersion = _reuseVersion;
            _didFadeComplete = false;

            try
            {
                _fadeTween = Tween.Alpha(_spriteRenderer, 1f, duration)
                    .OnComplete(this, target => target.MarkFadeCompleted());

                await _fadeTween;

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

                return _didFadeComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (IsCurrentReuse(reuseVersion))
                {
                    _fadeTween = default;
                }
            }
        }

        public async Task<MotionResult> PlayFadeOutAsync(float duration)
        {
            if (IsEmpty ||
                _fadeTween.isAlive ||
                !IsValidTime(duration))
            {
                return MotionResult.Failed;
            }

            int reuseVersion = _reuseVersion;
            _didFadeComplete = false;

            try
            {
                _fadeTween = Tween.Alpha(_spriteRenderer, 0f, duration)
                    .OnComplete(this, target => target.MarkFadeCompleted());

                await _fadeTween;

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

                return _didFadeComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (IsCurrentReuse(reuseVersion))
                {
                    _fadeTween = default;
                }
            }
        }

        public async Task<MotionResult> PlayRevealAsync()
        {
            if (IsEmpty || _isRevealPlaying || !IsValidTime(_revealDuration))
            {
                return MotionResult.Failed;
            }

            int reuseVersion = _reuseVersion;
            Vector3 targetScale = GetVisualScale(FoodItemVisualState.OnGrill);

            if (!IsValidScale(targetScale))
            {
                return MotionResult.Failed;
            }

            _isRevealPlaying = true;
            _didRevealComplete = false;
            transform.localScale = Vector3.zero;

            try
            {
                _revealTween = Tween.Scale(transform, targetScale, _revealDuration, _revealEase)
                    .OnComplete(this, target => target.MarkRevealCompleted());

                await _revealTween;

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

                return _didRevealComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (IsCurrentReuse(reuseVersion))
                {
                    _revealTween = default;
                    _isRevealPlaying = false;
                }
            }
        }

        public Task<MotionResult> PlayScaleAsync(
            Vector3 targetScale,
            float duration,
            Ease ease)
        {
            return PlayScaleAsync(
                targetScale,
                duration,
                ease,
                targetScale,
                0f,
                ease);
        }

        public async Task<MotionResult> PlayScaleAsync(
            Vector3 firstTargetScale,
            float firstDuration,
            Ease firstEase,
            Vector3 finalTargetScale,
            float finalDuration,
            Ease finalEase)
        {
            if (_scaleSequence.isAlive ||
                !IsValidScaleTarget(firstTargetScale) ||
                !IsValidScaleTarget(finalTargetScale) ||
                !IsValidTime(firstDuration) ||
                !IsValidTime(finalDuration))
            {
                return MotionResult.Failed;
            }

            int reuseVersion = _reuseVersion;
            _didScaleComplete = false;

            try
            {
                _scaleSequence = Sequence.Create()
                    .Chain(Tween.Scale(
                        transform,
                        firstTargetScale,
                        firstDuration,
                        firstEase));

                if (finalDuration > 0f)
                {
                    _scaleSequence = _scaleSequence.Chain(
                        Tween.Scale(
                            transform,
                            finalTargetScale,
                            finalDuration,
                            finalEase));
                }

                _scaleSequence = _scaleSequence.ChainCallback(
                    this,
                    target => target.MarkScaleCompleted());

                await _scaleSequence;

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

                return _didScaleComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (IsCurrentReuse(reuseVersion))
                {
                    _scaleSequence = default;
                }
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
            StopReveal(resetScale: true);
            StopScale();
            RestoreSortingLayerBeforeFlight();
        }

        public void PlayGrillSmoke()
        {
            Vector3 spawnPosition = transform.position + _grillSmokeOffset;
            _grillSmokePool.Play(spawnPosition);
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

            int reuseVersion = _reuseVersion;
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

                if (!IsCurrentReuse(reuseVersion))
                {
                    return MotionResult.Cancelled;
                }

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
                if (IsCurrentReuse(reuseVersion))
                {
                    _landingSequence = default;
                    _landingTarget = null;
                    _isLandingFeedbackPlaying = false;
                }
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

        public void ForceHiddenVisual()
        {
            SetSpriteAlpha(0f);
            transform.localScale = Vector3.zero;
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

        private void MarkRevealCompleted()
        {
            _didRevealComplete = true;
        }

        private void MarkLandingFeedbackCompleted()
        {
            _didLandingFeedbackComplete = true;
        }

        private void MarkScaleCompleted()
        {
            _didScaleComplete = true;
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

        private bool IsCurrentReuse(int reuseVersion)
        {
            return reuseVersion == _reuseVersion;
        }

        private void ResetViewState()
        {
            _reuseVersion++;
            CancelMotion();

            FoodTokenId = 0;
            IsInteractable = false;
            VisualState = FoodItemVisualState.Empty;
            Selected = null;

            _isFlying = false;
            _didFlightComplete = false;
            _didFadeComplete = false;
            _isRevealPlaying = false;
            _didRevealComplete = false;
            _isLandingFeedbackPlaying = false;
            _didLandingFeedbackComplete = false;
            _didScaleComplete = false;
            _flightTarget = null;
            _landingTarget = null;
            _flightTargetVisualState = null;

            _spriteRenderer.sprite = null;
            _spriteRenderer.enabled = false;
            SetSpriteAlpha(1f);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            ApplyColliderState();
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

            if (resetAlpha)
            {
                SetSpriteAlpha(1f);
            }
        }

        private void StopReveal(bool resetScale)
        {
            if (_revealTween.isAlive)
            {
                _revealTween.Stop();
            }

            _revealTween = default;

            if (resetScale && _isRevealPlaying)
            {
                transform.localScale = GetVisualScale(FoodItemVisualState.OnGrill);
            }

            _isRevealPlaying = false;
        }

        private void StopScale()
        {
            if (_scaleSequence.isAlive)
            {
                _scaleSequence.Stop();
            }

            _scaleSequence = default;
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
            if (!_hasFlyingSortingLayer)
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
            if (_hasSortingLayerBeforeFlight)
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

        private static bool IsValidScaleTarget(Vector3 value)
        {
            return IsValidScaleTargetValue(value.x) &&
                   IsValidScaleTargetValue(value.y) &&
                   IsValidScaleTargetValue(value.z);
        }

        private static bool IsValidScaleValue(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsValidScaleTargetValue(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void ApplyColliderState()
        {
            _clickCollider.enabled = !IsEmpty && IsInteractable;
        }

        private void ApplyVisualState()
        {
            if (IsEmpty)
            {
                VisualState = FoodItemVisualState.Empty;
                _spriteRenderer.enabled = false;
                return;
            }

            _spriteRenderer.enabled = true;
            transform.localScale = GetVisualScale(VisualState);
            transform.localRotation = GetVisualRotation(VisualState);
        }

        public Vector3 GetVisualScale(FoodItemVisualState visualState)
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
