namespace FoodieMatch.Core.Application.Events
{
    public readonly struct LevelEndedEvent
    {
        public int LevelNumber { get; }

        public bool IsWin { get; }

        public string Reason { get; }

        public LevelEndedEvent(
            int levelNumber,
            bool isWin,
            string reason)
        {
            LevelNumber = levelNumber;
            IsWin = isWin;
            Reason = reason;
        }
    }
}
