using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay.Booster
{
    public sealed class BoosterButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _badgeBackgroundImage;
        [SerializeField] private TMP_Text _countText;

        [Header("Badge Sprites")]
        [SerializeField] private Sprite _countBadgeSprite;
        [SerializeField] private Sprite _addBadgeSprite;

        private Action _useBoosterClicked;
        private Action _addBoosterClicked;
        private int _currentCount;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
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

        public void SetCount(int count)
        {
            _currentCount = Mathf.Max(0, count);

            bool hasBooster = _currentCount > 0;

            if (_badgeBackgroundImage != null)
            {
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

        private void OnButtonClicked()
        {
            if (_currentCount <= 0)
            {
                _addBoosterClicked?.Invoke();
                return;
            }

            _useBoosterClicked?.Invoke();
        }
    }
}
