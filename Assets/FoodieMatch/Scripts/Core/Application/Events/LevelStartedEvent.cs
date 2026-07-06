namespace FoodieMatch.Core.Application.Events
{
    public readonly struct LevelStartedEvent
    {
        public int LevelId { get; }

        public LevelStartedEvent(int levelId)
        {
            LevelId = levelId;
        }
    }
}
