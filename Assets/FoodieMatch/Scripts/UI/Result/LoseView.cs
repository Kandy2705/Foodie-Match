using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class LoseView : PopupBase, IPlayerResourceView
    {
        private const string LoseAnimationName = "lose";
        private const string IdleAnimationName = "idle";

        [Header("References")]
        [SerializeField] private Button _tryAgainButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private SkeletonGraphic _chefMascotSkeletonGraphic;
        [SerializeField] private ResourceBarView _resourceBarView;

        private Action _tryAgainClicked;
        private Action _homeClicked;

        private void Awake()
        {
            _tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
            _homeButton.onClick.AddListener(OnHomeButtonClicked);
        }

        private void OnDestroy()
        {
            _tryAgainButton.onClick.RemoveListener(OnTryAgainButtonClicked);
            _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
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

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
        }

        public void SetResourceClickActions(
            Action coinClicked,
            Action heartClicked)
        {
            _resourceBarView.SetResourceClickActions(
                coinClicked,
                heartClicked);
        }

        public override void Dispose()
        {
            _tryAgainClicked = null;
            _homeClicked = null;
            _resourceBarView.Clear();

            base.Dispose();
        }

        private void PlayLoseMascotAnimation()
        {
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

    }
}
