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

        private Vector3 _visibleScale;

        private void Awake()
        {
            _visibleScale = _motionRoot.localScale;
            StopEffects();
        }

        public void Bind(ClaimRewardItemData data)
        {
            _iconImage.sprite = data.Icon;
            _iconImage.preserveAspect = true;
            _amountText.text = data.AmountText;
            _amountText.gameObject.SetActive(
                !string.IsNullOrEmpty(data.AmountText));
            gameObject.SetActive(true);
        }

        public void PrepareReveal()
        {
            StopEffects();
            _motionRoot.localScale = Vector3.zero;
        }

        public Sequence InsertReveal(
            Sequence sequence,
            float startTime,
            float duration,
            Ease ease)
        {
            return sequence
                .InsertCallback(
                    startTime,
                    this,
                    view => view.PlayEffects())
                .Insert(
                    startTime,
                    Tween.Scale(
                        _motionRoot,
                        _visibleScale,
                        duration,
                        ease));
        }

        public void Hide()
        {
            StopEffects();
            _motionRoot.localScale = _visibleScale;
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
