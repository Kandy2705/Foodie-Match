namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public static class GameGoldPassProgressionDefaults
    {
        private const int DefaultUnlockLevel = 15;
        private const int DefaultNormalSpoonsPerCompletedLevel = 1;
        private const int DefaultHardSpoonsPerCompletedLevel = 2;
        private const int DefaultSuperHardSpoonsPerCompletedLevel = 3;

        public static GameGoldPassProgressionConfigSnapshot CreateSnapshot()
        {
            return new GameGoldPassProgressionConfigSnapshot(
                DefaultUnlockLevel,
                DefaultNormalSpoonsPerCompletedLevel,
                DefaultHardSpoonsPerCompletedLevel,
                DefaultSuperHardSpoonsPerCompletedLevel);
        }
    }
}
