using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class TrayView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform[] _foodAnchors;

        private Tween _fadeTween;
        private bool _didFadeComplete;
        private int _reuseVersion;

        private void OnDestroy()
        {
            StopFade(resetAlpha: false);
        }

        public void SetSortingOrder(int sortingOrder)
        {
            _spriteRenderer.sortingOrder = sortingOrder;
        }

        public Transform GetFoodAnchor(int index)
        {
            if (_foodAnchors == null || index < 0 || index >= _foodAnchors.Length)
            {
                return null;
            }

            return _foodAnchors[index];
        }

        public async Task<MotionResult> PlayFadeOutAsync(float duration)
        {
            if (_fadeTween.isAlive || !IsValidTime(duration))
            {
                return MotionResult.Failed;
            }

            int reuseVersion = _reuseVersion;
            _didFadeComplete = false;

            try
            {
                _fadeTween = Tween.Alpha(_spriteRenderer, 0f, duration)
                    .OnComplete(this, target => target.MarkFadeCompleted());

                await _fadeTween;

                if (reuseVersion != _reuseVersion)
                {
                    return MotionResult.Cancelled;
                }

                return _didFadeComplete
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                if (reuseVersion == _reuseVersion)
                {
                    _fadeTween = default;
                }
            }
        }

        public void ResetForUse()
        {
            _reuseVersion++;
            CancelMotion();
        }

        public void ResetForPool()
        {
            _reuseVersion++;
            CancelMotion();
        }

        public void CancelMotion()
        {
            StopFade(resetAlpha: true);
        }

        private void MarkFadeCompleted()
        {
            _didFadeComplete = true;
        }

        private void StopFade(bool resetAlpha)
        {
            if (_fadeTween.isAlive)
            {
                _fadeTween.Stop();
            }

            _fadeTween = default;

            if (!resetAlpha)
            {
                return;
            }

            Color color = _spriteRenderer.color;
            color.a = 1f;
            _spriteRenderer.color = color;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
