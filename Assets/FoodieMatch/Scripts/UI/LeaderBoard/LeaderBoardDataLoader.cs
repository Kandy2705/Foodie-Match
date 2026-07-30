using System;
using UnityEngine;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardDataLoader
    {
        private const string ResourcePath =
            "Leaderboard/leaderboard_players";

        public LeaderBoardDatabase Load()
        {
            TextAsset jsonAsset =
                Resources.Load<TextAsset>(ResourcePath);

            if (jsonAsset == null)
            {
                throw new InvalidOperationException(
                    $"Leaderboard JSON was not found at Resources/{ResourcePath}.json.");
            }

            LeaderBoardDatabase database =
                JsonUtility.FromJson<LeaderBoardDatabase>(
                    jsonAsset.text);

            if (database == null ||
                database.players == null ||
                database.players.Length == 0)
            {
                throw new InvalidOperationException(
                    "Leaderboard JSON does not contain players.");
            }

            if (string.IsNullOrWhiteSpace(database.currentAccountId) ||
                string.IsNullOrWhiteSpace(database.currentPlayerId))
            {
                throw new InvalidOperationException(
                    "Leaderboard JSON does not identify the current player.");
            }

            return database;
        }

        public LeaderBoardPlayerData FindCurrentPlayer(
            LeaderBoardDatabase database)
        {
            LeaderBoardPlayerData currentPlayer =
                Array.Find(
                    database.players,
                    player =>
                        player.playerId == database.currentPlayerId &&
                        player.accountId == database.currentAccountId);

            if (currentPlayer == null)
            {
                throw new InvalidOperationException(
                    $"Current leaderboard player was not found. " +
                    $"AccountId: {database.currentAccountId}, " +
                    $"PlayerId: {database.currentPlayerId}.");
            }

            return currentPlayer;
        }
    }
}
