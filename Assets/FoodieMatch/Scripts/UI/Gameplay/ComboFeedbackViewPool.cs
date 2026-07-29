using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class ComboFeedbackViewPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ComboFeedbackView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 4;
        [SerializeField, Min(1)] private int _maxRetainedCount = 8;

        private ComponentPool<ComboFeedbackView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<ComboFeedbackView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount);
        }

        public ComboFeedbackView Get(
            RectTransform parent,
            Vector2 anchoredPosition)
        {
            ComboFeedbackView feedbackView = _pool.Get(parent);
            RectTransform feedbackTransform =
                (RectTransform)feedbackView.transform;

            feedbackTransform.anchoredPosition = anchoredPosition;
            feedbackTransform.localRotation = Quaternion.identity;
            feedbackTransform.localScale = Vector3.one;
            feedbackTransform.SetAsLastSibling();
            return feedbackView;
        }

        public void Release(ComboFeedbackView feedbackView)
        {
            _pool.Release(feedbackView);
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}
