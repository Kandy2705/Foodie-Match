using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay.Booster
{
    public sealed class BoosterButtonView : MonoBehaviour
    {
        private const string LevelLockedFormat = "Lv.{0}";

        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _badgeBackgroundImage;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private TMP_Text _levelLockedText;

        [Header("Badge Sprites")]
        [SerializeField] private Sprite _countBadgeSprite;
        [SerializeField] private Sprite _addBadgeSprite;

        [Header("Locked Visual Targets")]
        [SerializeField] private Image _lockedButtonImage;
        [SerializeField] private Image _lockedIconImage;

        private Action _useBoosterClicked;
        private Action _addBoosterClicked;
        private int _currentCount;
        private int _unlockLevel = 1;
        private bool _isUnlocked = true;
        private Sprite _lockedButtonSprite;
        private Sprite _lockedIconSprite;
        private Sprite _defaultButtonSprite;
        private Sprite _defaultIconSprite;
        public bool IsUnlocked => _isUnlocked;
        public RectTransform RewardTarget => (RectTransform)transform;

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
            CacheDefaultSprites();
            ApplyLockedVisuals();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);

            _useBoosterClicked = null;
            _addBoosterClicked = null;
        }

        public void SetActions(
            Action useBoosterClicked,
            Action addBoosterClicked)
        {
            _useBoosterClicked = useBoosterClicked;
            _addBoosterClicked = addBoosterClicked;
        }

        public void SetLockedSprites(Sprite lockedButtonSprite, Sprite lockedIconSprite)
        {
            _lockedButtonSprite = lockedButtonSprite;
            _lockedIconSprite = lockedIconSprite;
            ApplyLockedVisuals();
        }

        public void SetUnlockLevel(int unlockLevel)
        {
            _unlockLevel = Mathf.Max(1, unlockLevel);
            ApplyLockedVisuals();
        }

        public void SetUnlocked(bool isUnlocked)
        {
            _isUnlocked = isUnlocked;
            ApplyLockedVisuals();
            RefreshBadgeVisuals();
        }

        public void SetCount(int count)
        {
            _currentCount = Mathf.Max(0, count);
            RefreshBadgeVisuals();
        }

        private void OnButtonClicked()
        {
            if (!_isUnlocked)
            {
                return;
            }

            if (_currentCount <= 0)
            {
                _addBoosterClicked?.Invoke();
                return;
            }

            _useBoosterClicked?.Invoke();
        }

        private void CacheDefaultSprites()
        {
            _defaultButtonSprite = _lockedButtonImage.sprite;
            _defaultIconSprite = _lockedIconImage.sprite;
        }

        private void ApplyLockedVisuals()
        {
            Sprite buttonSprite = !_isUnlocked && _lockedButtonSprite != null
                ? _lockedButtonSprite
                : _defaultButtonSprite;

            if (buttonSprite != null)
            {
                _lockedButtonImage.sprite = buttonSprite;
            }

            _lockedButtonImage.enabled = buttonSprite != null;

            if (!_isUnlocked)
            {
                if (_lockedIconSprite != null)
                {
                    _lockedIconImage.sprite = _lockedIconSprite;
                    _lockedIconImage.enabled = true;
                }
                else
                {
                    _lockedIconImage.enabled = false;
                }
            }
            else
            {
                if (_defaultIconSprite != null)
                {
                    _lockedIconImage.sprite = _defaultIconSprite;
                }

                _lockedIconImage.enabled = _defaultIconSprite != null;
            }

            if (!_isUnlocked)
            {
                _levelLockedText.gameObject.SetActive(true);
                _levelLockedText.text = string.Format(LevelLockedFormat, _unlockLevel);
            }
            else
            {
                _levelLockedText.gameObject.SetActive(false);
            }

            _button.interactable = _isUnlocked;
        }

        private void RefreshBadgeVisuals()
        {
            if (!_isUnlocked)
            {
                _badgeBackgroundImage.gameObject.SetActive(false);
                _countText.gameObject.SetActive(false);

                return;
            }

            bool hasBooster = _currentCount > 0;

            _badgeBackgroundImage.gameObject.SetActive(true);
            _badgeBackgroundImage.sprite = hasBooster
                ? _countBadgeSprite
                : _addBadgeSprite;
            _countText.gameObject.SetActive(hasBooster);
            _countText.text = hasBooster
                ? _currentCount.ToString()
                : string.Empty;
        }
    }
}
