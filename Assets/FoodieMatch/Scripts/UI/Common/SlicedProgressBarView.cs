using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Common
{
    public sealed class SlicedProgressBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        private RectTransform _fillRect;
        private float _fullWidth;

        private void Awake()
        {
            _fillRect = _fillImage.rectTransform;
            _fullWidth = _fillRect.rect.width;
        }

        public void SetProgress(float normalizedProgress)
        {
            float progress = Mathf.Clamp01(normalizedProgress);
            _fillImage.enabled = progress > 0f;
            _fillRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _fullWidth * progress);
        }
    }
}
