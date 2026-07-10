namespace FoodieMatch.Core.Application.Events
{
    public readonly struct LevelStartedEvent
    {
        public int LevelNumber { get; }

        public LevelStartedEvent(int levelNumber)
        {
            LevelNumber = levelNumber;
        }
    }
}
