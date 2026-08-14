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
            ResolveReferencesIfMissing();

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

        public void SetData(
            int currentLevel,
            string playerName = null,
            string joinDate = null,
            Sprite avatarSprite = null,
            int firstTryWins = 0,
            int hotPotWins = 0,
            int towerTrial = 0)
        {
            ResolveReferencesIfMissing();

            if (_levelValueText != null)
            {
                _levelValueText.text = currentLevel.ToString();
            }

            if (_playerNameText != null && !string.IsNullOrEmpty(playerName))
            {
                _playerNameText.text = playerName;
            }

            if (_joinDateText != null && !string.IsNullOrEmpty(joinDate))
            {
                _joinDateText.text = joinDate;
            }

            if (_avatarImage != null && avatarSprite != null)
            {
                _avatarImage.sprite = avatarSprite;
            }

            if (_firstTryWinsText != null)
            {
                _firstTryWinsText.text = firstTryWins.ToString();
            }

            if (_hotPotWinsText != null)
            {
                _hotPotWinsText.text = hotPotWins.ToString();
            }

            if (_towerTrialText != null)
            {
                _towerTrialText.text = towerTrial.ToString();
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

        private void ResolveReferencesIfMissing()
        {
            _popupAnimController ??= GetComponent<PopupAnimController>();

            if (_closeButton == null)
            {
                _closeButton = FindChildComponentByName<Button>("CloseButton");
            }

            if (_playerNameText == null)
            {
                _playerNameText = FindChildComponentByName<TMP_Text>("PlayerNameText");
            }

            if (_joinDateText == null)
            {
                _joinDateText = FindChildComponentByName<TMP_Text>("JoinDateText");
            }

            if (_levelValueText == null)
            {
                _levelValueText = FindChildComponentByName<TMP_Text>("LevelValue");
            }

            if (_avatarImage == null)
            {
                _avatarImage = FindChildComponentByName<Image>("AvatarImage");
            }

            if (_firstTryWinsText == null)
            {
                _firstTryWinsText = FindCardValueText("FirstTryWinsCard");
            }

            if (_hotPotWinsText == null)
            {
                _hotPotWinsText = FindCardValueText("HotPotWinsCard");
            }

            if (_towerTrialText == null)
            {
                _towerTrialText = FindCardValueText("TowerTrialCard");
            }
        }

        private T FindChildComponentByName<T>(string objectName) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].name == objectName || components[i].gameObject.name == objectName)
                {
                    return components[i];
                }
            }

            return null;
        }

        private TMP_Text FindCardValueText(string cardName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == cardName)
                {
                    TMP_Text[] texts = transforms[i].GetComponentsInChildren<TMP_Text>(true);
                    for (int j = 0; j < texts.Length; j++)
                    {
                        if (texts[j].name == "ValueText")
                        {
                            return texts[j];
                        }
                    }

                    if (texts.Length > 0)
                    {
                        return texts[texts.Length - 1];
                    }
                }
            }

            return null;
        }
    }
}
