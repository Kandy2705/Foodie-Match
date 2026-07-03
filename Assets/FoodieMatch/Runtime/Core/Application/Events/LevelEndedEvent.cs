namespace FoodieMatch.Runtime.Core.Application.Events
{
    public readonly struct LevelEndedEvent
    {
        public int LevelId { get; }

        public bool IsWin { get; }

        public string Reason { get; }

        public LevelEndedEvent(
            int levelId,
            bool isWin,
            string reason)
        {
            LevelId = levelId;
            IsWin = isWin;
            Reason = reason;
        }
    }
}
