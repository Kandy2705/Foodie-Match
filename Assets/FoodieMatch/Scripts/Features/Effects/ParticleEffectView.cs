using System;
using UnityEngine;

namespace FoodieMatch.Features.Effects
{
    public sealed class ParticleEffectView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particleSystem;

        private Action<ParticleEffectView> _completed;

        private void Awake()
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Play(Action<ParticleEffectView> completed)
        {
            _completed = completed;
            _particleSystem.Play(withChildren: true);
        }

        public void ResetForUse()
        {
            _completed = null;
            _particleSystem.Clear(withChildren: true);
        }

        public void ResetForPool()
        {
            _completed = null;
            _particleSystem.Stop(
                withChildren: true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnParticleSystemStopped()
        {
            Action<ParticleEffectView> completed = _completed;
            _completed = null;
            completed?.Invoke(this);
        }
    }
}
