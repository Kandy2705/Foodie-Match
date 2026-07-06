namespace FoodieMatch.Core.Application.Events
{
    public readonly struct LevelProgressChangedEvent
    {
        public int ServedCount { get; }

        public int TotalCount { get; }

        public LevelProgressChangedEvent(
            int servedCount,
            int totalCount)
        {
            ServedCount = servedCount;
            TotalCount = totalCount;
        }
    }
}
