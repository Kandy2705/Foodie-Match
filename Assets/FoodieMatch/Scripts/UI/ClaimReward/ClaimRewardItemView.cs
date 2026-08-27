using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.ClaimReward
{
    public sealed class ClaimRewardItemView : MonoBehaviour
    {
        private static readonly int NormalState =
            Animator.StringToHash("Normal");

        [SerializeField] private RectTransform _motionRoot;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private Animator _lightBurstAnimator;
        [SerializeField] private ParticleSystem _particleSystem;

        private bool _isInitialized;
        private Vector3 _visibleScale = Vector3.one;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            if (_motionRoot != null && _motionRoot.localScale != Vector3.zero)
            {
                _visibleScale = _motionRoot.localScale;
            }
            else
            {
                _visibleScale = Vector3.one;
            }

            StopEffects();
        }

        public void Bind(ClaimRewardItemData data)
        {
            EnsureInitialized();

            if (_iconImage != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.enabled = data.Icon != null;
                if (data.Icon != null)
                {
                    _iconImage.type = Image.Type.Simple;
                    _iconImage.SetNativeSize();
                }
            }

            if (_amountText != null)
            {
                _amountText.text = data.AmountText;
                _amountText.gameObject.SetActive(
                    !string.IsNullOrEmpty(data.AmountText));
            }

            gameObject.SetActive(true);
        }

        public void PrepareReveal()
        {
            EnsureInitialized();
            StopEffects();
            if (_motionRoot != null)
            {
                _motionRoot.localScale = Vector3.zero;
            }
        }

        public Sequence InsertReveal(
            Sequence sequence,
            float startTime,
            float duration,
            Ease ease)
        {
            EnsureInitialized();
            Vector3 targetScale = _visibleScale != Vector3.zero ? _visibleScale : Vector3.one;
            return sequence
                .InsertCallback(
                    startTime,
                    this,
                    view => view.PlayEffects())
                .Insert(
                    startTime,
                    Tween.Scale(
                        _motionRoot,
                        targetScale,
                        duration,
                        ease));
        }

        public void Hide()
        {
            EnsureInitialized();
            StopEffects();
            if (_motionRoot != null)
            {
                _motionRoot.localScale = _visibleScale != Vector3.zero ? _visibleScale : Vector3.one;
            }
            gameObject.SetActive(false);
        }

        private void PlayEffects()
        {
            _lightBurstAnimator.enabled = true;
            _lightBurstAnimator.Play(NormalState, 0, 0f);
            _lightBurstAnimator.Update(0f);
            _particleSystem.Play(true);
        }

        private void StopEffects()
        {
            _lightBurstAnimator.enabled = false;
            _particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
