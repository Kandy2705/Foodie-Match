using System;

namespace FoodieMatch.UI.LeaderBoard
{
    [Serializable]
    public sealed class LeaderBoardDatabase
    {
        public string currentAccountId;
        public string currentPlayerId;
        public LeaderBoardPlayerData[] players;
    }

    [Serializable]
    public sealed class LeaderBoardPlayerData
    {
        public string playerId;
        public string accountId;
        public string displayName;
        public string avatarId;
        public int level;
        public int weeklyScore;
        public int weeklyRank;
        public int globalRank;
    }
}
