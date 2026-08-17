using System;
using System.Collections.Generic;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfileCustomizationPopupView : PopupBase
    {
        private enum CustomizationTab
        {
            Avatar,
            Frame
        }

        [Header("Components")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        [Header("Player Info Preview")]
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Button _editNameButton;
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Image _previewAvatarImage;
        [SerializeField] private Image _previewFrameImage;

        [Header("Tabs")]
        [SerializeField] private Button _avatarTabButton;
        [SerializeField] private Button _frameTabButton;
        [SerializeField] private Image _avatarButtonImage;
        [SerializeField] private Image _frameButtonImage;
        [SerializeField] private Sprite _selectedTabSprite;
        [SerializeField] private Sprite _unselectedTabSprite;

        [Header("Content Areas")]
        [SerializeField] private GameObject _avatarContentRoot;
        [SerializeField] private Transform _avatarItemsContainer;
        [SerializeField] private GameObject _frameContentRoot;
        [SerializeField] private Transform _frameItemsContainer;

        [Header("Prefabs & Config")]
        [SerializeField] private AvatarItemView _avatarItemPrefab;
        [SerializeField] private FrameItemView _frameItemPrefab;
        [SerializeField] private ProfileCustomizationCatalogSO _catalog;

        private readonly List<AvatarItemView> _avatarItemViews = new();
        private readonly List<FrameItemView> _frameItemViews = new();

        private Action<string, string, string> _saveClicked;
        private Action _closeClicked;

        private string _workingPlayerName;
        private string _workingAvatarId;
        private string _workingFrameId;

        private CustomizationTab _currentTab =
            CustomizationTab.Avatar;

        private bool _isEditingName;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(
                    OnCloseButtonClicked);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(
                    OnSaveButtonClicked);
            }

            if (_avatarTabButton != null)
            {
                _avatarTabButton.onClick.AddListener(
                    OnAvatarTabClicked);
            }

            if (_frameTabButton != null)
            {
                _frameTabButton.onClick.AddListener(
                    OnFrameTabClicked);
            }

            if (_editNameButton != null)
            {
                _editNameButton.onClick.AddListener(
                    OnEditNameButtonClicked);
            }

            if (_nameInputField != null)
            {
                _nameInputField.onEndEdit.AddListener(
                    OnNameInputEndEdit);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(
                    OnCloseButtonClicked);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(
                    OnSaveButtonClicked);
            }

            if (_avatarTabButton != null)
            {
                _avatarTabButton.onClick.RemoveListener(
                    OnAvatarTabClicked);
            }

            if (_frameTabButton != null)
            {
                _frameTabButton.onClick.RemoveListener(
                    OnFrameTabClicked);
            }

            if (_editNameButton != null)
            {
                _editNameButton.onClick.RemoveListener(
                    OnEditNameButtonClicked);
            }

            if (_nameInputField != null)
            {
                _nameInputField.onEndEdit.RemoveListener(
                    OnNameInputEndEdit);
            }

            ClearSpawnedItems();
            Dispose();
        }

        public void SetActions(
            ProfileCustomizationPopupViewActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(
                    nameof(actions));
            }

            _saveClicked = actions.SaveClicked;
            _closeClicked = actions.CloseClicked;
        }

        public override void Setup(IPopupData data)
        {
            if (data is ProfileCustomizationPopupData customizationData)
            {
                if (customizationData.Catalog != null)
                {
                    _catalog = customizationData.Catalog;
                }

                _workingPlayerName =
                    customizationData.PlayerName;

                _workingAvatarId =
                    customizationData.AvatarId;

                _workingFrameId =
                    customizationData.FrameId;
            }

            if (_catalog != null)
            {
                if (string.IsNullOrEmpty(
                        _workingAvatarId))
                {
                    _workingAvatarId =
                        _catalog.DefaultAvatarId;
                }

                if (string.IsNullOrEmpty(
                        _workingFrameId))
                {
                    _workingFrameId =
                        _catalog.DefaultFrameId;
                }
            }

            if (_playerNameText != null)
            {
                _playerNameText.gameObject.SetActive(true);
            }

            if (_nameInputField != null)
            {
                _nameInputField.gameObject.SetActive(false);
            }

            _isEditingName = false;

            PopulateAvatarItems();
            PopulateFrameItems();

            SetTab(CustomizationTab.Avatar);

            UpdatePreview();
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
            if (gameObject.activeInHierarchy &&
                _popupAnimController != null)
            {
                _popupAnimController.Close(
                    OnCloseAnimationFinished);

                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _saveClicked = null;
            _closeClicked = null;

            base.Dispose();
        }

        private void PopulateAvatarItems()
        {
            ClearAvatarItems();

            if (_catalog == null ||
                _avatarItemPrefab == null)
            {
                return;
            }

            Transform container =
                _avatarItemsContainer != null
                    ? _avatarItemsContainer
                    : _avatarContentRoot != null
                        ? _avatarContentRoot.transform
                        : null;

            if (container == null)
            {
                return;
            }

            IReadOnlyList<ProfileCustomizationEntry> avatars =
                _catalog.Avatars;

            if (avatars == null)
            {
                return;
            }

            for (int i = 0; i < avatars.Count; i++)
            {
                ProfileCustomizationEntry avatarEntry =
                    avatars[i];

                if (avatarEntry == null)
                {
                    continue;
                }

                AvatarItemView itemView =
                    Instantiate(
                        _avatarItemPrefab,
                        container);

                bool isSelected =
                    string.Equals(
                        avatarEntry.Id,
                        _workingAvatarId,
                        StringComparison.OrdinalIgnoreCase);

                itemView.SetData(
                    avatarEntry.Id,
                    avatarEntry.Sprite,
                    isSelected,
                    OnAvatarSelected);

                _avatarItemViews.Add(
                    itemView);
            }
        }

        private void PopulateFrameItems()
        {
            ClearFrameItems();

            if (_catalog == null ||
                _frameItemPrefab == null)
            {
                return;
            }

            Transform container =
                _frameItemsContainer != null
                    ? _frameItemsContainer
                    : _frameContentRoot != null
                        ? _frameContentRoot.transform
                        : null;

            if (container == null)
            {
                return;
            }

            IReadOnlyList<ProfileCustomizationEntry> frames =
                _catalog.Frames;

            if (frames == null)
            {
                return;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                ProfileCustomizationEntry frameEntry =
                    frames[i];

                if (frameEntry == null)
                {
                    continue;
                }

                FrameItemView itemView =
                    Instantiate(
                        _frameItemPrefab,
                        container);

                bool isSelected =
                    string.Equals(
                        frameEntry.Id,
                        _workingFrameId,
                        StringComparison.OrdinalIgnoreCase);

                itemView.SetData(
                    frameEntry.Id,
                    frameEntry.Sprite,
                    isSelected,
                    OnFrameSelected);

                _frameItemViews.Add(
                    itemView);
            }
        }

        private void SetTab(
            CustomizationTab tab)
        {
            _currentTab = tab;

            bool isAvatar =
                tab == CustomizationTab.Avatar;

            if (_avatarContentRoot != null)
            {
                _avatarContentRoot.SetActive(
                    isAvatar);
            }

            if (_frameContentRoot != null)
            {
                _frameContentRoot.SetActive(
                    !isAvatar);
            }

            SetCustomizationTab(
                isAvatar);
        }

        private void SetCustomizationTab(
            bool isAvatarSelected)
        {
            if (isAvatarSelected)
            {
                SetTabVisual(
                    _avatarButtonImage,
                    _selectedTabSprite,
                    false);

                SetTabVisual(
                    _frameButtonImage,
                    _unselectedTabSprite,
                    false);

                return;
            }

            SetTabVisual(
                _avatarButtonImage,
                _unselectedTabSprite,
                true);

            SetTabVisual(
                _frameButtonImage,
                _selectedTabSprite,
                true);
        }

        private static void SetTabVisual(
            Image image,
            Sprite sprite,
            bool flipX)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            Vector3 scale =
                image.rectTransform.localScale;

            scale.x =
                flipX
                    ? -Mathf.Abs(scale.x)
                    : Mathf.Abs(scale.x);

            image.rectTransform.localScale =
                scale;
        }

        private void OnAvatarTabClicked()
        {
            if (_currentTab ==
                CustomizationTab.Avatar)
            {
                return;
            }

            SetTab(
                CustomizationTab.Avatar);
        }

        private void OnFrameTabClicked()
        {
            if (_currentTab ==
                CustomizationTab.Frame)
            {
                return;
            }

            SetTab(
                CustomizationTab.Frame);
        }

        private void OnAvatarSelected(
            string avatarId)
        {
            _workingAvatarId =
                avatarId;

            for (int i = 0;
                 i < _avatarItemViews.Count;
                 i++)
            {
                AvatarItemView view =
                    _avatarItemViews[i];

                if (view == null)
                {
                    continue;
                }

                view.SetSelected(
                    string.Equals(
                        view.AvatarId,
                        avatarId,
                        StringComparison.OrdinalIgnoreCase));
            }

            UpdatePreview();
        }

        private void OnFrameSelected(
            string frameId)
        {
            _workingFrameId =
                frameId;

            for (int i = 0;
                 i < _frameItemViews.Count;
                 i++)
            {
                FrameItemView view =
                    _frameItemViews[i];

                if (view == null)
                {
                    continue;
                }

                view.SetSelected(
                    string.Equals(
                        view.FrameId,
                        frameId,
                        StringComparison.OrdinalIgnoreCase));
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (!_isEditingName)
            {
                if (_playerNameText != null &&
                    !string.IsNullOrEmpty(
                        _workingPlayerName))
                {
                    _playerNameText.text =
                        _workingPlayerName;
                }

                if (_nameInputField != null &&
                    !string.IsNullOrEmpty(
                        _workingPlayerName))
                {
                    _nameInputField.SetTextWithoutNotify(
                        _workingPlayerName);
                }
            }

            if (_catalog == null)
            {
                return;
            }

            if (_previewAvatarImage != null)
            {
                Sprite avatarSprite =
                    _catalog.GetAvatarSpriteOrDefault(
                        _workingAvatarId);

                if (avatarSprite != null)
                {
                    _previewAvatarImage.sprite =
                        avatarSprite;
                }
            }

            if (_previewFrameImage != null)
            {
                Sprite frameSprite =
                    _catalog.GetFrameSpriteOrDefault(
                        _workingFrameId);

                if (frameSprite != null)
                {
                    _previewFrameImage.sprite =
                        frameSprite;
                }
            }
        }

        private void OnEditNameButtonClicked()
        {
            if (_nameInputField == null)
            {
                Debug.LogError(
                    "[ProfileCustomizationPopupView] _nameInputField is null. Assign it in the Inspector.");
                return;
            }

            _isEditingName = true;

            if (_playerNameText != null)
            {
                _playerNameText.gameObject.SetActive(false);
            }

            _nameInputField.gameObject.SetActive(true);
            _nameInputField.interactable = true;
            _nameInputField.readOnly = false;
            _nameInputField.SetTextWithoutNotify(_workingPlayerName);

            StartCoroutine(FocusInputFieldNextFrame());
        }

        private System.Collections.IEnumerator FocusInputFieldNextFrame()
        {
            yield return null;

            if (_nameInputField == null ||
                !_nameInputField.gameObject.activeInHierarchy)
            {
                yield break;
            }

            _nameInputField.Select();
            _nameInputField.ActivateInputField();
            _nameInputField.caretPosition =
                _nameInputField.text.Length;

            UnityEngine.EventSystems.EventSystem.current
                ?.SetSelectedGameObject(
                    _nameInputField.gameObject);
        }

        private void OnNameInputEndEdit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _workingPlayerName = text.Trim();
            }

            _isEditingName = false;

            _nameInputField.gameObject.SetActive(false);

            if (_playerNameText != null)
            {
                _playerNameText.gameObject.SetActive(true);
                _playerNameText.text = _workingPlayerName;
            }
        }

        private void OnSaveButtonClicked()
        {
            _saveClicked?.Invoke(
                _workingPlayerName,
                _workingAvatarId,
                _workingFrameId);
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

        private void ClearAvatarItems()
        {
            for (int i = 0;
                 i < _avatarItemViews.Count;
                 i++)
            {
                if (_avatarItemViews[i] != null)
                {
                    Destroy(
                        _avatarItemViews[i]
                            .gameObject);
                }
            }

            _avatarItemViews.Clear();
        }

        private void ClearFrameItems()
        {
            for (int i = 0;
                 i < _frameItemViews.Count;
                 i++)
            {
                if (_frameItemViews[i] != null)
                {
                    Destroy(
                        _frameItemViews[i]
                            .gameObject);
                }
            }

            _frameItemViews.Clear();
        }

        private void ClearSpawnedItems()
        {
            ClearAvatarItems();
            ClearFrameItems();
        }
    }
}
