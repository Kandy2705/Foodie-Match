using System;
using System.Threading.Tasks;
using FoodieMatch.UI.Popup;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace FoodieMatch.UI.Booster
{
    public sealed class BoosterSwapPopup : PopupBase
    {
        [SerializeField] private SkeletonGraphic _skeletonGraphic;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private TrackEntry _trackEntry;
        private TaskCompletionSource<bool> _animationTcs;

        private void OnDisable()
        {
            CompleteAnimation(false);
        }

        public override void Show()
        {
            base.Show();
            _canvasGroup.alpha = 1f;
        }

        public Task StartSwapAnimationAsync()
        {
            if (!_skeletonGraphic.IsValid)
            {
                _skeletonGraphic.Initialize(overwrite: false);
            }

            if (_skeletonGraphic.AnimationState == null)
            {
                return Task.CompletedTask;
            }

            CompleteAnimation(false);

            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;

            _animationTcs = new TaskCompletionSource<bool>();

            _skeletonGraphic.AnimationState.ClearTracks();
            _trackEntry = _skeletonGraphic.AnimationState.SetAnimation(
                0, "swap_booster", loop: false);
            _trackEntry.Complete += HandleAnimationCompleted;

            return _animationTcs.Task;
        }

        public async Task WaitForAnimationProgressAsync(
            float normalizedProgress)
        {
            normalizedProgress = Mathf.Clamp01(
                normalizedProgress);

            TrackEntry entry = _trackEntry;

            if (entry == null)
            {
                return;
            }

            float animationDuration =
                entry.AnimationEnd - entry.AnimationStart;

            if (animationDuration <= 0f)
            {
                return;
            }

            float targetTime =
                entry.AnimationStart +
                animationDuration * normalizedProgress;

            while (_trackEntry == entry &&
                   entry.AnimationTime < targetTime)
            {
                await Task.Yield();
            }
        }

        public async Task HideAsync()
        {
            float elapsed = 0f;

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutDuration);
                await Task.Yield();
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void HandleAnimationCompleted(TrackEntry trackEntry)
        {
            if (trackEntry != _trackEntry)
            {
                return;
            }

            CompleteAnimation(true);
        }

        private void CompleteAnimation(bool completed)
        {
            StopListeningForCompletion();
            _animationTcs?.TrySetResult(completed);
            _animationTcs = null;
        }

        private void StopListeningForCompletion()
        {
            if (_trackEntry != null)
            {
                _trackEntry.Complete -= HandleAnimationCompleted;
                _trackEntry = null;
            }
        }
    }
}
