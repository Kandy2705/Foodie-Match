using System;
using System.Collections.Generic;
using System.Linq;
using FoodieMatch.Core.Application.Leaderboard;
using FoodieMatch.Infrastructure.Persistence.Save;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.Leaderboard
{
    public sealed class PlayerPrefsLeaderboardPendingSubmissionStore :
        ILeaderboardPendingSubmissionStore
    {
        private const string SaveKey = "Leaderboard.PendingSubmissions";

        private readonly ISaveService _saveService;

        public PlayerPrefsLeaderboardPendingSubmissionStore(
            ISaveService saveService)
        {
            _saveService = saveService;
        }

        public bool TryLoad(
            out IReadOnlyList<LeaderboardCompletion> completions)
        {
            if (!_saveService.HasKey(SaveKey))
            {
                completions = Array.Empty<LeaderboardCompletion>();
                return true;
            }

            try
            {
                string json = _saveService.GetString(SaveKey, null);
                LeaderboardCompletionDto[] items =
                    JsonConvert.DeserializeObject<LeaderboardCompletionDto[]>(json);
                completions = items
                    .Select(MapToCompletion)
                    .ToArray();
                return true;
            }
            catch
            {
                completions = Array.Empty<LeaderboardCompletion>();
                return false;
            }
        }

        public bool TrySave(
            IReadOnlyList<LeaderboardCompletion> completions)
        {
            try
            {
                if (completions.Count == 0)
                {
                    _saveService.DeleteKey(SaveKey);
                }
                else
                {
                    string json = JsonConvert.SerializeObject(
                        completions.Select(MapToDto).ToArray(),
                        Formatting.None);
                    _saveService.SetString(SaveKey, json);
                }

                _saveService.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static LeaderboardCompletion MapToCompletion(
            LeaderboardCompletionDto dto)
        {
            return new LeaderboardCompletion(
                dto.CompletionId,
                dto.LevelNumber,
                DateTimeOffset.FromUnixTimeMilliseconds(
                    dto.CompletedAtUnixMilliseconds),
                dto.WeekId,
                dto.PlayerName,
                dto.AvatarId,
                dto.FrameId);
        }

        private static LeaderboardCompletionDto MapToDto(
            LeaderboardCompletion completion)
        {
            return new LeaderboardCompletionDto
            {
                CompletionId = completion.CompletionId,
                LevelNumber = completion.LevelNumber,
                CompletedAtUnixMilliseconds = completion.CompletedAtUtc
                    .ToUnixTimeMilliseconds(),
                WeekId = completion.WeekId,
                PlayerName = completion.PlayerName,
                AvatarId = completion.AvatarId,
                FrameId = completion.FrameId
            };
        }

        private sealed class LeaderboardCompletionDto
        {
            [JsonProperty("completionId")]
            public string CompletionId { get; set; }

            [JsonProperty("levelNumber")]
            public int LevelNumber { get; set; }

            [JsonProperty("completedAtUnixMilliseconds")]
            public long CompletedAtUnixMilliseconds { get; set; }

            [JsonProperty("weekId")]
            public string WeekId { get; set; }

            [JsonProperty("playerName")]
            public string PlayerName { get; set; }

            [JsonProperty("avatarId")]
            public string AvatarId { get; set; }

            [JsonProperty("frameId")]
            public string FrameId { get; set; }
        }
    }
}
