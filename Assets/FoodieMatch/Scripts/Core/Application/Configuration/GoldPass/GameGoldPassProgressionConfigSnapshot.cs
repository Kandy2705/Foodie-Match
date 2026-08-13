using System;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GameGoldPassProgressionConfigSnapshot :
        IGameGoldPassProgressionConfig
    {
        public GameGoldPassProgressionConfigSnapshot(
            int spoonsPerCompletedLevel)
        {
            if (spoonsPerCompletedLevel <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spoonsPerCompletedLevel));
            }

            SpoonsPerCompletedLevel = spoonsPerCompletedLevel;
        }

        public int SpoonsPerCompletedLevel { get; }
    }
}
