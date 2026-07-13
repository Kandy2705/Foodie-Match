using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageSlotView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _previewAlpha = 0.45f;
        [SerializeField] private float _filledAlpha = 1f;

        [Header("Motion")]
        [SerializeField] private Vector3 _landingPunchStrength =
            new Vector3(0.1f, 0.1f, 0f);
        [SerializeField] private float _landingFeedbackDuration = 0.16f;
        [SerializeField] private Vector3 _completePunchStrength =
            new Vector3(0.16f, 0.16f, 0f);
        [SerializeField] private float _completeFeedbackDuration = 0.22f;

        private Tween _feedbackTween;
        private bool _hasInitialLocalScale;
        private Vector3 _initialLocalScale;

        public bool IsVisible { get; private set; }
        public bool IsFilled { get; private set; }
        public Vector3 WorldPosition => transform.position;

        private void Awake()
        {
            EnsureInitialLocalScale();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            ApplyVisibility();
        }

        private void OnDestroy()
        {
            StopFeedback(resetScale: false);
        }

        public void Show(Sprite sprite, bool isFilled)
        {
            StopFeedback(resetScale: true);
            IsVisible = true;
            IsFilled = isFilled;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }

            ApplyVisibility();
        }

        public void Hide()
        {
            StopFeedback(resetScale: true);
            IsVisible = false;
            IsFilled = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = null;
            }

            ApplyVisibility();
        }

        public void SetFilled()
        {
            if (!IsVisible)
            {
                return;
            }

            IsFilled = true;
            ApplyVisibility();
        }

        public void PlayLandingFeedback()
        {
            PlayFeedback(
                _landingPunchStrength,
                _landingFeedbackDuration);
        }

        public void PlayCompleteFeedback()
        {
            PlayFeedback(
                _completePunchStrength,
                _completeFeedbackDuration);
        }

        private void ApplyVisibility()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled =
                    IsVisible && _spriteRenderer.sprite != null;
                _spriteRenderer.color = GetCurrentColor();
            }
        }

        private Color GetCurrentColor()
        {
            if (_spriteRenderer == null)
            {
                return Color.white;
            }

            Color color = _spriteRenderer.color;
            color.a = IsFilled ? _filledAlpha : _previewAlpha;
            return color;
        }

        private void PlayFeedback(
            Vector3 punchStrength,
            float duration)
        {
            if (!IsVisible || duration <= 0f)
            {
                return;
            }

            StopFeedback(resetScale: true);
            _feedbackTween = Tween.PunchScale(
                transform,
                punchStrength,
                duration);
        }

        private void StopFeedback(bool resetScale)
        {
            if (_feedbackTween.isAlive)
            {
                _feedbackTween.Stop();
            }

            _feedbackTween = default;

            if (resetScale)
            {
                EnsureInitialLocalScale();
                transform.localScale = _initialLocalScale;
            }
        }

        private void EnsureInitialLocalScale()
        {
            if (_hasInitialLocalScale)
            {
                return;
            }

            _initialLocalScale = transform.localScale;
            _hasInitialLocalScale = true;
        }
    }
}
