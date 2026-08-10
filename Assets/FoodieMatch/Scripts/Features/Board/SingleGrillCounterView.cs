using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class SingleGrillCounterView : MonoBehaviour
    {
        [SerializeField] private GameObject _counterRoot;
        [SerializeField] private TMP_Text _hiddenFoodCountText;
        [SerializeField] private Transform _remainingFoodScaleObject;
        [SerializeField] private Transform _dividerTemplate;
        [SerializeField] private SpriteRenderer _counterBarRenderer;

        private readonly List<Transform> _dividers = new();
        private int _initialHiddenFoodCount;

        public void Setup(int initialHiddenFoodCount)
        {
            _initialHiddenFoodCount = initialHiddenFoodCount;
            SetupDividers(initialHiddenFoodCount);
            SetRemainingFoodCount(initialHiddenFoodCount);
        }

        public void SetRemainingFoodCount(int remainingHiddenFoodCount)
        {
            float visibleRatio = _initialHiddenFoodCount == 0
                ? 0f
                : remainingHiddenFoodCount / (float)_initialHiddenFoodCount;
            SetVisibleRatio(visibleRatio);

            bool isVisible = remainingHiddenFoodCount > 0;
            _counterRoot.SetActive(isVisible);

            if (!isVisible)
            {
                return;
            }

            _hiddenFoodCountText.SetText(remainingHiddenFoodCount.ToString());
        }

        public void ResetCounter()
        {
            _initialHiddenFoodCount = 0;
            SetVisibleRatio(0f);
            _counterRoot.SetActive(false);

            for (int i = 0; i < _dividers.Count; i++)
            {
                _dividers[i].gameObject.SetActive(false);
            }
        }

        private void SetupDividers(int hiddenFoodCount)
        {
            if (_dividers.Count == 0)
            {
                _dividers.Add(_dividerTemplate);
            }

            int requiredDividerCount = Mathf.Max(0, hiddenFoodCount - 1);

            while (_dividers.Count < requiredDividerCount)
            {
                Transform divider = Instantiate(
                    _dividerTemplate,
                    _dividerTemplate.parent);
                _dividers.Add(divider);
            }

            float dividerAreaWidth = _counterBarRenderer.size.x;
            float leftEdge = -dividerAreaWidth * 0.5f;

            for (int i = 0; i < _dividers.Count; i++)
            {
                bool isRequired = i < requiredDividerCount;
                Transform divider = _dividers[i];
                divider.gameObject.SetActive(isRequired);

                if (!isRequired)
                {
                    continue;
                }

                float progress = (i + 1f) / hiddenFoodCount;
                Vector3 position = divider.localPosition;
                position.x = leftEdge + dividerAreaWidth * progress;
                divider.localPosition = position;
            }
        }

        private void SetVisibleRatio(float visibleRatio)
        {
            Vector3 scale = _remainingFoodScaleObject.localScale;
            scale.x = visibleRatio;
            _remainingFoodScaleObject.localScale = scale;
        }
    }
}
