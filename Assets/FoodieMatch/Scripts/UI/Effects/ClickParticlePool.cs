using System.Collections.Generic;
using FoodieMatch.Features.Effects;
using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.UI.Effects
{
    public sealed class ClickParticlePool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ParticleEffectView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 8;
        [SerializeField, Min(1)] private int _maxRetainedCount = 16;

        private readonly List<ParticleEffectView> _activeEffects = new();
        private ComponentPool<ParticleEffectView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<ParticleEffectView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForUse: PrepareForUse,
                prepareForPool: PrepareForPool);
        }

        public void Play(
            RectTransform effectRoot,
            Vector2 screenPosition)
        {
            Canvas canvas = effectRoot.GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectRoot,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition);

            ParticleEffectView effectView = _pool.Get(effectRoot);
            RectTransform effectTransform =
                (RectTransform)effectView.transform;
            effectTransform.anchoredPosition = localPosition;
            effectTransform.localRotation = Quaternion.identity;
            effectTransform.localScale = Vector3.one;
            effectTransform.SetAsLastSibling();
            _activeEffects.Add(effectView);
            effectView.Play(Release);
        }

        public void ReleaseAll()
        {
            while (_activeEffects.Count > 0)
            {
                Release(_activeEffects[^1]);
            }
        }

        public void Clear()
        {
            ReleaseAll();
            _pool.Clear();
        }

        private void Release(ParticleEffectView effectView)
        {
            _activeEffects.Remove(effectView);
            _pool.Release(effectView);
        }

        private static void PrepareForUse(ParticleEffectView effectView)
        {
            effectView.ResetForUse();
        }

        private static void PrepareForPool(ParticleEffectView effectView)
        {
            effectView.ResetForPool();
        }
    }
}
