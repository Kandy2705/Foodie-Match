using System;
using UnityEngine;

namespace FoodieMatch.Core.Application.GameState
{
    /// <summary>
    /// Tracks consecutive order completions within a time window.
    /// Progress is the remaining window ratio for the HUD fill bar.
    /// </summary>
    public sealed class ComboProgressModel
    {
        private readonly float _windowDuration;
        private float _remainingTime;

        public ComboProgressModel(float windowDuration)
        {
            if (windowDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(windowDuration));
            }

            _windowDuration = windowDuration;
        }

        public int ComboCount { get; private set; }

        public float FillNormalized { get; private set; }

        public bool IsActive => ComboCount > 0 && _remainingTime > 0f;

        public void Reset()
        {
            ComboCount = 0;
            _remainingTime = 0f;
            FillNormalized = 0f;
        }

        public void RegisterOrderCompleted()
        {
            ComboCount++;
            _remainingTime = _windowDuration;
            FillNormalized = 1f;
        }

        /// <summary>
        /// Advances the combo timer. Returns true when the combo chain just expired.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return false;
            }

            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            _remainingTime -= deltaTime;

            if (_remainingTime <= 0f)
            {
                Reset();
                return true;
            }

            FillNormalized = Mathf.Clamp01(_remainingTime / _windowDuration);
            return false;
        }
    }
}
