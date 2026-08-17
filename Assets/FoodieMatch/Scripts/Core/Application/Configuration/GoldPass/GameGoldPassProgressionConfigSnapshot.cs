using System;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GameGoldPassProgressionConfigSnapshot :
        IGameGoldPassProgressionConfig
    {
        public GameGoldPassProgressionConfigSnapshot(
            int normalSpoonsPerCompletedLevel,
            int hardSpoonsPerCompletedLevel,
            int superHardSpoonsPerCompletedLevel)
        {
            ValidatePositiveValue(
                normalSpoonsPerCompletedLevel,
                nameof(normalSpoonsPerCompletedLevel));
            ValidatePositiveValue(
                hardSpoonsPerCompletedLevel,
                nameof(hardSpoonsPerCompletedLevel));
            ValidatePositiveValue(
                superHardSpoonsPerCompletedLevel,
                nameof(superHardSpoonsPerCompletedLevel));

            NormalSpoonsPerCompletedLevel = normalSpoonsPerCompletedLevel;
            HardSpoonsPerCompletedLevel = hardSpoonsPerCompletedLevel;
            SuperHardSpoonsPerCompletedLevel =
                superHardSpoonsPerCompletedLevel;
        }

        public int NormalSpoonsPerCompletedLevel { get; }

        public int HardSpoonsPerCompletedLevel { get; }

        public int SuperHardSpoonsPerCompletedLevel { get; }

        public int GetSpoonsPerCompletedLevel(LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Normal => NormalSpoonsPerCompletedLevel,
                LevelDifficulty.Hard => HardSpoonsPerCompletedLevel,
                LevelDifficulty.SuperHard => SuperHardSpoonsPerCompletedLevel,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(difficulty),
                    difficulty,
                    "Level difficulty is not defined.")
            };
        }

        private static void ValidatePositiveValue(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
