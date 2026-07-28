using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using LevelPlaySdk = Unity.Services.LevelPlay.LevelPlay;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class LevelPlayAdsInitializer
    {
        private readonly string _appKey;

        private InitializationState _state;

        public LevelPlayAdsInitializer(string appKey)
        {
            _appKey = appKey;
            LevelPlaySdk.OnInitSuccess += OnInitializationSucceeded;
            LevelPlaySdk.OnInitFailed += OnInitializationFailed;
        }

        public event Action Initialized;

        public bool IsInitialized =>
            _state == InitializationState.Initialized;

        public void Initialize()
        {
            if (_state != InitializationState.NotStarted)
            {
                return;
            }

            _state = InitializationState.Initializing;
            LevelPlaySdk.Init(_appKey);
        }

        private void OnInitializationSucceeded(
            LevelPlayConfiguration configuration)
        {
            _state = InitializationState.Initialized;
            Initialized?.Invoke();
        }

        private void OnInitializationFailed(LevelPlayInitError error)
        {
            _state = InitializationState.NotStarted;
            Debug.LogError($"LevelPlay initialization failed: {error}");
        }

        private enum InitializationState
        {
            NotStarted,
            Initializing,
            Initialized
        }
    }
}
