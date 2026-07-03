using System;
namespace FoodieMatch.Runtime.Core.Application.Events
{
    public sealed class GameplayEvents
    {
        public event Action<LevelStartedEvent> LevelStarted;
        public event Action<LevelProgressChangedEvent> LevelProgressChanged;
        public event Action<LevelEndedEvent> LevelEnded;
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
    }
}
