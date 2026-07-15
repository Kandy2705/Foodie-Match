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

        [Header("Motion")]
        [SerializeField] private Vector3 _landingPunchStrength =
            new Vector3(0.1f, 0.1f, 0f);
        [SerializeField] private float _landingFeedbackDuration = 0.16f;

        private Tween _flightTween;
        private Tween _landingFeedbackTween;
        private bool _isFlying;
        private bool _didFlightComplete;

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

        public async Task<MotionResult> PlayFlightAsync(
            Vector3 targetPosition,
            float duration,
            float startDelay = 0f)
        {
            if (!CanStartFlight(duration, startDelay))
            {
                return MotionResult.Failed;
            }

            _isFlying = true;
            _didFlightComplete = false;
            SetInteractable(false);

            try
            {
                _flightTween = Tween.Position(
                        transform,
                        targetPosition,
                        duration,
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

            if (_landingFeedbackTween.isAlive)
            {
                _landingFeedbackTween.Stop();
            }

            _landingFeedbackTween = default;
        }

        public void PlayLandingFeedback()
        {
            if (IsEmpty ||
                !IsValidTime(_landingFeedbackDuration) ||
                _landingFeedbackDuration == 0f)
            {
                return;
            }

            if (_landingFeedbackTween.isAlive)
            {
                _landingFeedbackTween.Stop();
            }

            _landingFeedbackTween = Tween.PunchScale(
                transform,
                _landingPunchStrength,
                _landingFeedbackDuration);
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

        private bool CanStartFlight(
            float duration,
            float startDelay)
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

        private static bool IsValidTime(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
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
