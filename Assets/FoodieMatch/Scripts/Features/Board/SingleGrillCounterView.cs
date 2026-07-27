using TMPro;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class SingleGrillCounterView : MonoBehaviour
    {
        private const int MaximumHiddenFoodCount = 3;

        [SerializeField] private GameObject _counterRoot;
        [SerializeField] private TMP_Text _hiddenFoodCountText;
        [SerializeField] private Transform _remainingFoodScaleObject;

        private Vector3 _remainingFoodFullScale;
        private bool _hasRemainingFoodFullScale;

        public bool TrySetHiddenFoodCount(int hiddenFoodCount)
        {
            if (hiddenFoodCount < 0 || hiddenFoodCount > MaximumHiddenFoodCount)
            {
                Debug.LogError(
                    $"Hidden food count must be between 0 and {MaximumHiddenFoodCount}.",
                    this);
                return false;
            }

            if (!_hasRemainingFoodFullScale)
            {
                _remainingFoodFullScale = _remainingFoodScaleObject.localScale;
                _hasRemainingFoodFullScale = true;
            }

            float visibleRatio = hiddenFoodCount / (float)MaximumHiddenFoodCount;
            Vector3 scale = _remainingFoodFullScale;
            scale.x *= visibleRatio;
            _remainingFoodScaleObject.localScale = scale;

            bool isVisible = hiddenFoodCount > 0;
            _counterRoot.SetActive(isVisible);

            if (!isVisible)
            {
                return true;
            }

            _hiddenFoodCountText.SetText(hiddenFoodCount.ToString());
            return true;
        }
    }
}
