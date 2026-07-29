using System.Collections.Generic;
using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class ComboFeedbackViewPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ComboFeedbackView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 4;
        [SerializeField, Min(1)] private int _maxRetainedCount = 8;

        private readonly List<ComboFeedbackView> _activeViews = new();
        private ComponentPool<ComboFeedbackView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<ComboFeedbackView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForUse: PrepareForUse,
                prepareForPool: PrepareForPool);
        }

        public void Play(
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
            _activeViews.Add(feedbackView);
            feedbackView.PlayRandomAnimation(Release);
        }

        public void ReleaseAll()
        {
            while (_activeViews.Count > 0)
            {
                Release(_activeViews[_activeViews.Count - 1]);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private void Release(ComboFeedbackView feedbackView)
        {
            _activeViews.Remove(feedbackView);
            _pool.Release(feedbackView);
        }

        private static void PrepareForUse(
            ComboFeedbackView feedbackView)
        {
            feedbackView.ResetForUse();
        }

        private static void PrepareForPool(
            ComboFeedbackView feedbackView)
        {
            feedbackView.ResetForPool();
        }
    }
}
