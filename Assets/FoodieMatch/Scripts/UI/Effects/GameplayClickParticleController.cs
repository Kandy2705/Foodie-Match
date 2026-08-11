using System;
using FoodieMatch.Features.Gameplay;
using UnityEngine;

namespace FoodieMatch.UI.Effects
{
    public sealed class GameplayClickParticleController
    {
        private readonly Action<Vector2> _playEffect;
        private bool _isEffectEnabled;

        public GameplayClickParticleController(
            GameplayPointerInput pointerInput,
            Action<Vector2> playEffect)
        {
            _playEffect = playEffect;
            pointerInput.PointerPressed += OnPointerPressed;
        }

        public void SetEffectEnabled(bool effectEnabled)
        {
            _isEffectEnabled = effectEnabled;
        }

        private void OnPointerPressed(Vector2 screenPosition)
        {
            if (_isEffectEnabled)
            {
                _playEffect(screenPosition);
            }
        }
    }
}
