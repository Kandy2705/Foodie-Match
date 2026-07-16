using System;

namespace FoodieMatch.Core.Application.Events
{
    public sealed class GameplayEvents
    {
        public event Action<LevelStartedEvent> LevelStarted;

        public event Action<LevelProgressChangedEvent> LevelProgressChanged;

        public event Action<LevelEndedEvent> LevelEnded;

        public event Action<ComboChangedEvent> ComboChanged;

        public void OnLevelStarted(LevelStartedEvent eventData)
        {
            LevelStarted?.Invoke(eventData);
        }

        public void OnLevelProgressChanged(LevelProgressChangedEvent eventData)
        {
            LevelProgressChanged?.Invoke(eventData);
        }

        public void OnLevelEnded(LevelEndedEvent eventData)
        {
            LevelEnded?.Invoke(eventData);
        }

        public void OnComboChanged(ComboChangedEvent eventData)
        {
            ComboChanged?.Invoke(eventData);
        }
    }
}
