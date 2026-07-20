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
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private TrackEntry _trackEntry;
        private TaskCompletionSource<bool> _animationTcs;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            EnsureSkeletonGraphic();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            StopListeningForCompletion();
        }

        private void OnDisable()
        {
            StopListeningForCompletion();
        }

        public override void Show()
        {
            base.Show();
            _canvasGroup.alpha = 1f;
        }

        public async Task PlaySwapAnimationAsync()
        {
            EnsureSkeletonGraphic();

            if (_skeletonGraphic == null)
            {
                Debug.LogError("BoosterSwapPopup SkeletonGraphic is missing.", this);
                return;
            }

            if (!_skeletonGraphic.IsValid)
            {
                _skeletonGraphic.Initialize(overwrite: false);
            }

            if (_skeletonGraphic.AnimationState == null)
            {
                Debug.LogError("BoosterSwapPopup AnimationState is missing.", this);
                return;
            }

            StopListeningForCompletion();

            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;

            _animationTcs = new TaskCompletionSource<bool>();
            TrackEntry trackEntry = null;

            try
            {
                _skeletonGraphic.AnimationState.ClearTracks();
                _trackEntry = _skeletonGraphic.AnimationState.SetAnimation(
                    0, "swap_booster", loop: false);
                _trackEntry.Complete += HandleAnimationCompleted;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopListeningForCompletion();
                return;
            }

            await _animationTcs.Task;
        }

        public async Task HideAsync()
        {
            if (_canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

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

            StopListeningForCompletion();
            _animationTcs?.TrySetResult(true);
        }

        private void StopListeningForCompletion()
        {
            if (_trackEntry != null)
            {
                _trackEntry.Complete -= HandleAnimationCompleted;
                _trackEntry = null;
            }
        }

        private void EnsureSkeletonGraphic()
        {
            if (_skeletonGraphic == null)
            {
                _skeletonGraphic = GetComponentInChildren<SkeletonGraphic>(includeInactive: true);
            }
        }
    }
}
