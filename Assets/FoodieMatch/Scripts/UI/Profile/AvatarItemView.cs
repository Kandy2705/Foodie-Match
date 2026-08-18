using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Profile
{
    public sealed class AvatarItemView : MonoBehaviour
    {
        [SerializeField] private Image _avatarImage;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private Button _button;

        private Action<string> _onClick;

        public string AvatarId { get; private set; }

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

            _onClick = null;
        }

        public void SetData(
            string avatarId,
            Sprite sprite,
            bool isSelected,
            Action<string> onClick)
        {
            AvatarId = avatarId;
            _onClick = onClick;

            if (_avatarImage != null && sprite != null)
            {
                _avatarImage.sprite = sprite;
            }

            SetSelected(isSelected);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(isSelected);
            }
        }

        private void OnButtonClicked()
        {
            if (!string.IsNullOrEmpty(AvatarId))
            {
                _onClick?.Invoke(AvatarId);
            }
        }
    }
}
