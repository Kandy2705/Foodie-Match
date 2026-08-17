using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.GoldPass;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassRewardPreviewView : MonoBehaviour
    {
        private static readonly int NormalState =
            Animator.StringToHash("Normal");
        private static readonly int OpenTrigger = Animator.StringToHash("Open");
        private static readonly int CloseTrigger =
            Animator.StringToHash("Close");

        [SerializeField] private Animator _animator;
        [SerializeField] private RectTransform _pointerImage;
        [SerializeField] private GoldPassRewardItemView[] _rewardItems;
        [SerializeField] private float _freePointerX = -74f;
        [SerializeField] private float _seasonPointerX = 74f;
        [SerializeField, Min(0f)] private float _visibleDuration = 3f;
        [SerializeField, Min(0f)] private float _closeDuration = 0.5f;

        private Tween _hideTween;
        private Tween _deactivateTween;
        private int _sourceMilestoneLevel;
        private GoldPassTrack _sourceTrack;
        private bool _isVisible;
        private bool _isClosing;

        public void Toggle(
            int milestoneLevel,
            GoldPassTrack track,
            GoldPassRewardDefinition treasure,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            if (_isVisible &&
                !_isClosing &&
                _sourceMilestoneLevel == milestoneLevel &&
                _sourceTrack == track)
            {
                BeginHide();
                return;
            }

            StopHideTimer();
            StopDeactivateTimer();
            _sourceMilestoneLevel = milestoneLevel;
            _sourceTrack = track;
            _isVisible = true;
            _isClosing = false;

            Vector2 pointerPosition = _pointerImage.anchoredPosition;
            pointerPosition.x = track == GoldPassTrack.Free
                ? _freePointerX
                : _seasonPointerX;
            _pointerImage.anchoredPosition = pointerPosition;

            for (int i = 0; i < _rewardItems.Length; i++)
            {
                if (i < treasure.Contents.Count)
                {
                    _rewardItems[i].Bind(
                        treasure.Contents[i],
                        visualCatalog);
                    continue;
                }

                _rewardItems[i].gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlayOpenAnimation();
            _hideTween = Tween.Delay(
                this,
                _visibleDuration,
                view => view.BeginHide(),
                useUnscaledTime: true);
        }

        private void PlayOpenAnimation()
        {
            _animator.ResetTrigger(CloseTrigger);
            _animator.ResetTrigger(OpenTrigger);
            _animator.Play(NormalState, 0, 0f);
            _animator.Update(0f);
            _animator.SetTrigger(OpenTrigger);
        }

        private void BeginHide()
        {
            StopHideTimer();

            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            _animator.ResetTrigger(OpenTrigger);
            _animator.ResetTrigger(CloseTrigger);
            _animator.SetTrigger(CloseTrigger);
            _deactivateTween = Tween.Delay(
                this,
                _closeDuration,
                view => view.Hide(),
                useUnscaledTime: true);
        }

        public void Hide()
        {
            StopHideTimer();
            StopDeactivateTimer();
            _isVisible = false;
            _isClosing = false;
            _animator.ResetTrigger(OpenTrigger);
            _animator.ResetTrigger(CloseTrigger);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            StopHideTimer();
            StopDeactivateTimer();
        }

        private void StopHideTimer()
        {
            if (_hideTween.isAlive)
            {
                _hideTween.Stop();
            }

            _hideTween = default;
        }

        private void StopDeactivateTimer()
        {
            if (_deactivateTween.isAlive)
            {
                _deactivateTween.Stop();
            }

            _deactivateTween = default;
        }
    }
}
