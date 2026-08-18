using System;
using System.Collections.Generic;
using System.Linq;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.GoldPass;
using FoodieMatch.Core.Domain.Heart;
using FoodieMatch.Core.Domain.Player;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    internal sealed class PlayerProfileMapper
    {
        public bool TryMapToRecord(
            PlayerProfileDto profileDto,
            out PlayerProfileRecord record,
            out string errorMessage)
        {
            if (profileDto == null)
            {
                throw new ArgumentNullException(nameof(profileDto));
            }

            if (profileDto.Revision < 0)
            {
                record = null;
                errorMessage = "Player profile revision cannot be negative.";
                return false;
            }

            if (!TryMapBoosterCounts(
                    profileDto.BoosterCounts,
                    out Dictionary<BoosterType, int> boosterCounts,
                    out errorMessage) ||
                !TryMapSeenBoosterGuides(
                    profileDto.SeenBoosterGuides,
                    out List<BoosterType> seenBoosterGuides,
                    out errorMessage) ||
                !TryMapHeartState(
                    profileDto,
                    out HeartState heartState,
                    out errorMessage) ||
                !TryMapGoldPassState(
                    profileDto.GoldPass,
                    out GoldPassState goldPassState,
                    out errorMessage))
            {
                record = null;
                return false;
            }

            try
            {
                PlayerProfile profile = new(
                    profileDto.CurrentLevelNumber,
                    profileDto.CoinBalance,
                    boosterCounts,
                    seenBoosterGuides,
                    heartState,
                    goldPassState,
                    profileDto.AdsRemoved,
                    profileDto.UnlimitedHeartEndUnixSeconds,
                    profileDto.FirstTryWins,
                    profileDto.HasFailedCurrentLevel,
                    profileDto.CreatedAtUnixSeconds,
                    profileDto.PlayerName,
                    profileDto.AvatarId,
                    profileDto.FrameId);
                record = new PlayerProfileRecord(profile, profileDto.Revision);
                errorMessage = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                record = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        public PlayerProfileDto MapToDto(PlayerProfile profile, long revision)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision),
                    revision,
                    "Revision cannot be negative.");
            }

            return new PlayerProfileDto
            {
                SchemaVersion = PlayerProfileDataVersions.Current,
                Revision = revision,
                CurrentLevelNumber = profile.CurrentLevelNumber,
                CoinBalance = profile.CoinBalance,
                FirstTryWins = profile.FirstTryWins,
                HasFailedCurrentLevel = profile.HasFailedCurrentLevel,
                CreatedAtUnixSeconds = profile.CreatedAtUnixSeconds,
                PlayerName = profile.PlayerName,
                AvatarId = profile.AvatarId,
                FrameId = profile.FrameId,
                HeartCount = profile.HeartState.HeartCount,
                HeartRecoveryStartedAtUtcUnixSeconds =
                    profile.HeartState.RecoveryStartedAtUtc?
                        .ToUnixTimeSeconds(),
                BoosterCounts = profile.BoosterCounts
                    .OrderBy(boosterCount => (int)boosterCount.Key)
                    .Select(
                        boosterCount => new BoosterCountDto
                        {
                            BoosterType = (int)boosterCount.Key,
                            Count = boosterCount.Value
                        })
                    .ToList(),
                SeenBoosterGuides = profile.SeenBoosterGuides
                    .Select(boosterType => (int)boosterType)
                    .OrderBy(boosterType => boosterType)
                    .ToList(),
                AdsRemoved = profile.AdsRemoved,
                UnlimitedHeartEndUnixSeconds =
                    profile.UnlimitedHeartEndUnixSeconds,
                GoldPass = new GoldPassStateDto
                {
                    SeasonId = profile.GoldPassState.SeasonId,
                    SpoonCount = profile.GoldPassState.SpoonCount,
                    IsSeasonPassPurchased =
                        profile.GoldPassState.IsSeasonPassPurchased,
                    ClaimedFreeMilestoneLevels = profile.GoldPassState
                        .ClaimedFreeMilestoneLevels
                        .OrderBy(level => level)
                        .ToList(),
                    ClaimedSeasonMilestoneLevels = profile.GoldPassState
                        .ClaimedSeasonMilestoneLevels
                        .OrderBy(level => level)
                        .ToList()
                }
            };
        }

        private static bool TryMapGoldPassState(
            GoldPassStateDto goldPassDto,
            out GoldPassState goldPassState,
            out string errorMessage)
        {
            if (goldPassDto == null)
            {
                goldPassState = null;
                errorMessage = "Player profile Gold Pass state is missing.";
                return false;
            }

            try
            {
                goldPassState = new GoldPassState(
                    goldPassDto.SeasonId,
                    goldPassDto.SpoonCount,
                    goldPassDto.IsSeasonPassPurchased,
                    goldPassDto.ClaimedFreeMilestoneLevels,
                    goldPassDto.ClaimedSeasonMilestoneLevels);
                errorMessage = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                goldPassState = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryMapBoosterCounts(
            IReadOnlyCollection<BoosterCountDto> boosterCountDtos,
            out Dictionary<BoosterType, int> boosterCounts,
            out string errorMessage)
        {
            boosterCounts = new Dictionary<BoosterType, int>();

            if (boosterCountDtos == null)
            {
                errorMessage = "Player profile booster counts are missing.";
                return false;
            }

            foreach (BoosterCountDto boosterCountDto in boosterCountDtos)
            {
                if (boosterCountDto == null)
                {
                    errorMessage = "Player profile contains an empty booster count entry.";
                    return false;
                }

                BoosterType boosterType = (BoosterType)boosterCountDto.BoosterType;

                if (!Enum.IsDefined(typeof(BoosterType), boosterType))
                {
                    errorMessage =
                        $"Booster type {boosterCountDto.BoosterType} is not defined.";
                    return false;
                }

                if (boosterCountDto.Count < 0)
                {
                    errorMessage = $"Booster {boosterType} count cannot be negative.";
                    return false;
                }

                if (!boosterCounts.TryAdd(boosterType, boosterCountDto.Count))
                {
                    errorMessage = $"Booster {boosterType} count is duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryMapHeartState(
            PlayerProfileDto profileDto,
            out HeartState heartState,
            out string errorMessage)
        {
            try
            {
                DateTimeOffset? recoveryStartedAtUtc =
                    profileDto.HeartRecoveryStartedAtUtcUnixSeconds.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(
                            profileDto.HeartRecoveryStartedAtUtcUnixSeconds.Value)
                        : null;

                heartState = new HeartState(
                    profileDto.HeartCount,
                    recoveryStartedAtUtc);
                errorMessage = null;
                return true;
            }
            catch (ArgumentOutOfRangeException exception)
            {
                heartState = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryMapSeenBoosterGuides(
            IReadOnlyCollection<int> boosterTypeValues,
            out List<BoosterType> seenBoosterGuides,
            out string errorMessage)
        {
            seenBoosterGuides = new List<BoosterType>();

            if (boosterTypeValues == null)
            {
                errorMessage = "Player profile seen booster guides are missing.";
                return false;
            }

            HashSet<BoosterType> uniqueBoosterTypes = new();

            foreach (int boosterTypeValue in boosterTypeValues)
            {
                BoosterType boosterType = (BoosterType)boosterTypeValue;

                if (!Enum.IsDefined(typeof(BoosterType), boosterType))
                {
                    errorMessage = $"Booster guide type {boosterTypeValue} is not defined.";
                    return false;
                }

                if (!uniqueBoosterTypes.Add(boosterType))
                {
                    errorMessage = $"Booster guide {boosterType} is duplicated.";
                    return false;
                }

                seenBoosterGuides.Add(boosterType);
            }

            errorMessage = null;
            return true;
        }
    }
}
