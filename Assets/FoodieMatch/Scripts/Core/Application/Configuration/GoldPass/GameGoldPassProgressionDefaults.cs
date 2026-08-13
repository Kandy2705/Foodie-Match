namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public static class GameGoldPassProgressionDefaults
    {
        private const int DefaultSpoonsPerCompletedLevel = 1;

        public static GameGoldPassProgressionConfigSnapshot CreateSnapshot()
        {
            return new GameGoldPassProgressionConfigSnapshot(
                DefaultSpoonsPerCompletedLevel);
        }
    }
}
