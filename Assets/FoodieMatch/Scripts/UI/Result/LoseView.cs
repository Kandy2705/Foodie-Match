using System;
using FoodieMatch.UI.Popup;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class LoseView : PopupBase
    {
        private const string LoseAnimationName = "lose";
        private const string IdleAnimationName = "idle";

        [Header("References")]
        [SerializeField] private Button _tryAgainButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private SkeletonGraphic _chefMascotSkeletonGraphic;

        private Action _tryAgainClicked;
        private Action _homeClicked;

        private void Awake()
        {
            EnsureMascotReference();

            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.RemoveListener(OnTryAgainButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }
        }

        public override void Show()
        {
            base.Show();
            PlayLoseMascotAnimation();
        }

        public void SetActions(LoseViewActions actions)
        {
            _tryAgainClicked = actions.TryAgainClicked;
            _homeClicked = actions.HomeClicked;
        }

        public override void Dispose()
        {
            _tryAgainClicked = null;
            _homeClicked = null;

            base.Dispose();
        }

        private void PlayLoseMascotAnimation()
        {
            EnsureMascotReference();

            if (_chefMascotSkeletonGraphic == null)
            {
                return;
            }

            if (!_chefMascotSkeletonGraphic.IsValid)
            {
                _chefMascotSkeletonGraphic.Initialize(overwrite: false);
            }

            if (_chefMascotSkeletonGraphic.AnimationState == null)
            {
                Debug.LogWarning(
                    $"{nameof(LoseView)} on {name} has no AnimationState on chef mascot.",
                    this);
                return;
            }

            _chefMascotSkeletonGraphic.AnimationState.ClearTracks();
            _chefMascotSkeletonGraphic.AnimationState.SetAnimation(
                0,
                LoseAnimationName,
                loop: false);
            _chefMascotSkeletonGraphic.AnimationState.AddAnimation(
                0,
                IdleAnimationName,
                loop: true,
                delay: 0f);
        }

        private void OnTryAgainButtonClicked()
        {
            _tryAgainClicked?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _homeClicked?.Invoke();
        }

        private void EnsureMascotReference()
        {
            if (_chefMascotSkeletonGraphic != null)
            {
                return;
            }

            _chefMascotSkeletonGraphic =
                GetComponentInChildren<SkeletonGraphic>(includeInactive: true);
        }
    }
}
