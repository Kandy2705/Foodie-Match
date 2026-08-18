using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Profile
{
    public sealed class FrameItemView : MonoBehaviour
    {
        [SerializeField] private Image _frameImage;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private Button _button;

        private Action<string> _onClick;

        public string FrameId { get; private set; }

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
            string frameId,
            Sprite sprite,
            bool isSelected,
            Action<string> onClick)
        {
            FrameId = frameId;
            _onClick = onClick;

            if (_frameImage != null && sprite != null)
            {
                _frameImage.sprite = sprite;
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
            if (!string.IsNullOrEmpty(FrameId))
            {
                _onClick?.Invoke(FrameId);
            }
        }
    }
}
