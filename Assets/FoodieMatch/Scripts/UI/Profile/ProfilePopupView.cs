using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfilePopupView : PopupBase
    {
        [Header("Components")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _editAvatarButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        [Header("Profile Info")]
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _joinDateText;
        [SerializeField] private TMP_Text _levelValueText;
        [SerializeField] private Image _avatarImage;

        [Header("Stats Cards")]
        [SerializeField] private TMP_Text _firstTryWinsText;
        [SerializeField] private TMP_Text _hotPotWinsText;
        [SerializeField] private TMP_Text _towerTrialText;

        private Action _closeClicked;
        private Action _editAvatarClicked;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_editAvatarButton != null)
            {
                _editAvatarButton.onClick.AddListener(OnEditAvatarButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_editAvatarButton != null)
            {
                _editAvatarButton.onClick.RemoveListener(OnEditAvatarButtonClicked);
            }

            Dispose();
        }

        public void SetActions(ProfilePopupViewActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            _closeClicked = actions.CloseClicked;
            _editAvatarClicked = actions.EditAvatarClicked;
        }

        public override void Setup(IPopupData data)
        {
            if (data is not ProfilePopupData profileData)
            {
                return;
            }

            if (_levelValueText != null)
            {
                _levelValueText.text = profileData.CurrentLevel.ToString();
            }

            if (_playerNameText != null && !string.IsNullOrEmpty(profileData.PlayerName))
            {
                _playerNameText.text = profileData.PlayerName;
            }

            if (_joinDateText != null && !string.IsNullOrEmpty(profileData.JoinDate))
            {
                _joinDateText.text = profileData.JoinDate;
            }

            if (_avatarImage != null && profileData.AvatarSprite != null)
            {
                _avatarImage.sprite = profileData.AvatarSprite;
            }

            if (_firstTryWinsText != null)
            {
                _firstTryWinsText.text = profileData.FirstTryWins.ToString();
            }

            if (_hotPotWinsText != null)
            {
                _hotPotWinsText.text = profileData.HotPotWins.ToString();
            }

            if (_towerTrialText != null)
            {
                _towerTrialText.text = profileData.TowerTrial.ToString();
            }
        }

        public override void Show()
        {
            base.Show();

            if (_popupAnimController != null)
            {
                _popupAnimController.Open();
            }
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy && _popupAnimController != null)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _editAvatarClicked = null;

            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnEditAvatarButtonClicked()
        {
            _editAvatarClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
