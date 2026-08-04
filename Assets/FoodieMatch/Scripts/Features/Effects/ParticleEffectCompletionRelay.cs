using System;
using UnityEngine;

namespace FoodieMatch.Features.Effects
{
    public sealed class ParticleEffectCompletionRelay : MonoBehaviour
    {
        private Action _completed;

        public void SetCompletedAction(Action completed)
        {
            _completed = completed;
        }

        private void OnParticleSystemStopped()
        {
            _completed?.Invoke();
        }
    }
}
