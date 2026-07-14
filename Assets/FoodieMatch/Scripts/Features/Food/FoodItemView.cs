using System;
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
        private Action<FoodItemView> _onFlightCompleted;

        public int FoodTokenId { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsInteractable { get; private set; }
        public bool IsFlying => _flightTween.isAlive;
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

        public bool TryPlayFlight(
            Vector3 targetPosition,
            float duration,
            Action<FoodItemView> onFlightCompleted)
        {
            if (IsEmpty || IsFlying || duration < 0f)
            {
                return false;
            }

            SetInteractable(false);
            _onFlightCompleted = onFlightCompleted;
            _flightTween = Tween
                .Position(
                    transform,
                    targetPosition,
                    duration)
                .OnComplete(
                    target: this,
                    target => target.OnFlightCompleted());

            return true;
        }

        public void CancelFlight()
        {
            _onFlightCompleted = null;

            if (_flightTween.isAlive)
            {
                _flightTween.Stop();
            }

            _flightTween = default;
        }

        public void CancelMotion()
        {
            CancelFlight();

            if (_landingFeedbackTween.isAlive)
            {
                _landingFeedbackTween.Stop();
            }

            _landingFeedbackTween = default;
        }

        public void PlayLandingFeedback()
        {
            if (IsEmpty || _landingFeedbackDuration <= 0f)
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

        private void OnFlightCompleted()
        {
            _flightTween = default;

            Action<FoodItemView> onFlightCompleted =
                _onFlightCompleted;
            _onFlightCompleted = null;
            onFlightCompleted?.Invoke(this);
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
