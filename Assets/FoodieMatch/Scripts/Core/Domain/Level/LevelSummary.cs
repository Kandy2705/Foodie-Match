namespace FoodieMatch.Core.Domain.Level
{
    public readonly struct LevelSummary
    {
        public LevelSummary(
            int levelNumber,
            LevelDifficulty difficulty)
        {
            LevelNumber = levelNumber;
            Difficulty = difficulty;
        }

        public int LevelNumber { get; }

        public LevelDifficulty Difficulty { get; }
    }
}
