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
        [Tooltip("Button background Image (shared unlocked art is cached from this).")]
        [SerializeField] private Image _lockedButtonImage;
        [Tooltip("Icon Image (unlocked art is cached from this; locked icon comes from catalog).")]
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
        private bool _didCacheDefaultSprites;

        public bool IsUnlocked => _isUnlocked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }

            EnsureLevelLockedTextReference();
            CacheDefaultSprites();
            ApplyLockedVisuals();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }

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
            if (_didCacheDefaultSprites)
            {
                return;
            }

            if (_lockedButtonImage != null)
            {
                _defaultButtonSprite = _lockedButtonImage.sprite;
            }

            if (_lockedIconImage != null)
            {
                _defaultIconSprite = _lockedIconImage.sprite;
            }

            _didCacheDefaultSprites = true;
        }

        private void ApplyLockedVisuals()
        {
            CacheDefaultSprites();
            EnsureLevelLockedTextReference();

            if (_lockedButtonImage != null)
            {
                Sprite buttonSprite = !_isUnlocked && _lockedButtonSprite != null
                    ? _lockedButtonSprite
                    : _defaultButtonSprite;

                if (buttonSprite != null)
                {
                    _lockedButtonImage.sprite = buttonSprite;
                }

                _lockedButtonImage.enabled = buttonSprite != null;
            }

            if (_lockedIconImage != null)
            {
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
            }

            if (_levelLockedText != null)
            {
                if (!_isUnlocked)
                {
                    _levelLockedText.gameObject.SetActive(true);
                    _levelLockedText.text = string.Format(LevelLockedFormat, _unlockLevel);
                }
                else
                {
                    _levelLockedText.gameObject.SetActive(false);
                }
            }

            if (_button != null)
            {
                _button.interactable = _isUnlocked;
            }
        }

        private void RefreshBadgeVisuals()
        {
            if (!_isUnlocked)
            {
                if (_badgeBackgroundImage != null)
                {
                    _badgeBackgroundImage.gameObject.SetActive(false);
                }

                if (_countText != null)
                {
                    _countText.gameObject.SetActive(false);
                }

                return;
            }

            bool hasBooster = _currentCount > 0;

            if (_badgeBackgroundImage != null)
            {
                _badgeBackgroundImage.gameObject.SetActive(true);
                _badgeBackgroundImage.sprite = hasBooster
                    ? _countBadgeSprite
                    : _addBadgeSprite;
            }

            if (_countText != null)
            {
                _countText.gameObject.SetActive(hasBooster);
                _countText.text = hasBooster
                    ? _currentCount.ToString()
                    : string.Empty;
            }
        }

        private void EnsureLevelLockedTextReference()
        {
            if (_levelLockedText != null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text != null && text.gameObject.name == "LevelLockedText")
                {
                    _levelLockedText = text;
                    return;
                }
            }
        }
    }
}
