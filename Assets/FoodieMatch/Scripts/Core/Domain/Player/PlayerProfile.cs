using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.GoldPass;
using FoodieMatch.Core.Domain.Heart;

namespace FoodieMatch.Core.Domain.Player
{
    public sealed class PlayerProfile
    {
        private const string DefaultPlayerName = "Kandy";
        private const string DefaultAvatarId = "avatar_01";
        private const string DefaultFrameId = "frame_01";

        private readonly ReadOnlyDictionary<BoosterType, int> _boosterCounts;
        private readonly ReadOnlyCollection<BoosterType> _seenBoosterGuides;
        private readonly HashSet<BoosterType> _seenBoosterGuideSet;

        public PlayerProfile(
            int currentLevelNumber,
            long coinBalance,
            IReadOnlyDictionary<BoosterType, int> boosterCounts,
            IReadOnlyCollection<BoosterType> seenBoosterGuides,
            HeartState heartState,
            GoldPassState goldPassState,
            bool adsRemoved = false,
            long unlimitedHeartEndUnixSeconds = 0,
            int firstTryWins = 0,
            bool hasFailedCurrentLevel = false,
            long createdAtUnixSeconds = 0,
            string playerName = null,
            string avatarId = null,
            string frameId = null)
        {
            if (currentLevelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevelNumber),
                    currentLevelNumber,
                    "Current level number must be at least 1.");
            }

            if (coinBalance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coinBalance),
                    coinBalance,
                    "Coin balance cannot be negative.");
            }

            if (unlimitedHeartEndUnixSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unlimitedHeartEndUnixSeconds),
                    unlimitedHeartEndUnixSeconds,
                    "Unlimited heart end time cannot be negative.");
            }

            if (firstTryWins < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstTryWins),
                    firstTryWins,
                    "First try wins cannot be negative.");
            }

            if (createdAtUnixSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(createdAtUnixSeconds),
                    createdAtUnixSeconds,
                    "Created at timestamp cannot be negative.");
            }

            CurrentLevelNumber = currentLevelNumber;
            CoinBalance = coinBalance;
            AdsRemoved = adsRemoved;
            UnlimitedHeartEndUnixSeconds = unlimitedHeartEndUnixSeconds;
            FirstTryWins = firstTryWins;
            HasFailedCurrentLevel = hasFailedCurrentLevel;
            CreatedAtUnixSeconds = createdAtUnixSeconds;
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim();
            AvatarId = string.IsNullOrWhiteSpace(avatarId) ? DefaultAvatarId : avatarId.Trim();
            FrameId = string.IsNullOrWhiteSpace(frameId) ? DefaultFrameId : frameId.Trim();
            HeartState = heartState ??
                throw new ArgumentNullException(nameof(heartState));
            GoldPassState = goldPassState ??
                throw new ArgumentNullException(nameof(goldPassState));
            _boosterCounts = CopyBoosterCounts(boosterCounts);
            _seenBoosterGuideSet = CopySeenBoosterGuides(seenBoosterGuides);
            _seenBoosterGuides = new ReadOnlyCollection<BoosterType>(
                new List<BoosterType>(_seenBoosterGuideSet));
        }

        public int CurrentLevelNumber { get; }

        public long CoinBalance { get; }

        public bool AdsRemoved { get; }

        public long UnlimitedHeartEndUnixSeconds { get; }

        public int FirstTryWins { get; }

        public bool HasFailedCurrentLevel { get; }

        public long CreatedAtUnixSeconds { get; }

        public string PlayerName { get; }

        public string AvatarId { get; }

        public string FrameId { get; }

        public HeartState HeartState { get; }

        public GoldPassState GoldPassState { get; }

        public IReadOnlyDictionary<BoosterType, int> BoosterCounts => _boosterCounts;

        public IReadOnlyCollection<BoosterType> SeenBoosterGuides => _seenBoosterGuides;

        public int GetBoosterCount(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));
            return _boosterCounts.TryGetValue(boosterType, out int count)
                ? count
                : 0;
        }

        public bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));
            return _seenBoosterGuideSet.Contains(boosterType);
        }

        public PlayerProfile WithCurrentLevelNumber(int currentLevelNumber)
        {
            if (currentLevelNumber == CurrentLevelNumber)
            {
                return this;
            }

            return new PlayerProfile(
                currentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                hasFailedCurrentLevel: false,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithCoinBalance(long coinBalance)
        {
            if (coinBalance == CoinBalance)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                coinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithBoosterCount(
            BoosterType boosterType,
            int count)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Booster count cannot be negative.");
            }

            if (GetBoosterCount(boosterType) == count)
            {
                return this;
            }

            Dictionary<BoosterType, int> boosterCounts = new(_boosterCounts)
            {
                [boosterType] = count
            };

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithSeenBoosterGuide(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));

            if (HasSeenBoosterGuide(boosterType))
            {
                return this;
            }

            List<BoosterType> seenBoosterGuides = new(_seenBoosterGuides)
            {
                boosterType
            };

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithHeartState(HeartState heartState)
        {
            if (heartState == null)
            {
                throw new ArgumentNullException(nameof(heartState));
            }

            if (ReferenceEquals(HeartState, heartState))
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                heartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithResourceState(
            long coinBalance,
            IReadOnlyDictionary<BoosterType, int> boosterCounts,
            HeartState heartState,
            bool adsRemoved,
            long unlimitedHeartEndUnixSeconds)
        {
            return new PlayerProfile(
                CurrentLevelNumber,
                coinBalance,
                boosterCounts,
                _seenBoosterGuides,
                heartState,
                GoldPassState,
                adsRemoved,
                unlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithAdsRemoved(bool adsRemoved)
        {
            if (adsRemoved == AdsRemoved)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                adsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithFirstTryWins(int firstTryWins)
        {
            if (firstTryWins == FirstTryWins)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                firstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithFailedCurrentLevel()
        {
            if (HasFailedCurrentLevel)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                hasFailedCurrentLevel: true,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithResetFailedCurrentLevel()
        {
            if (!HasFailedCurrentLevel)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                hasFailedCurrentLevel: false,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithPlayerName(string playerName)
        {
            string resolvedName = string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim();
            if (resolvedName == PlayerName)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                resolvedName,
                AvatarId,
                FrameId);
        }

        public PlayerProfile WithAvatarId(string avatarId)
        {
            string resolvedAvatarId = string.IsNullOrWhiteSpace(avatarId) ? DefaultAvatarId : avatarId.Trim();
            if (resolvedAvatarId == AvatarId)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                resolvedAvatarId,
                FrameId);
        }

        public PlayerProfile WithFrameId(string frameId)
        {
            string resolvedFrameId = string.IsNullOrWhiteSpace(frameId) ? DefaultFrameId : frameId.Trim();
            if (resolvedFrameId == FrameId)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                resolvedFrameId);
        }

        public PlayerProfile WithCustomization(
            string playerName,
            string avatarId,
            string frameId)
        {
            string resolvedName = string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim();
            string resolvedAvatarId = string.IsNullOrWhiteSpace(avatarId) ? DefaultAvatarId : avatarId.Trim();
            string resolvedFrameId = string.IsNullOrWhiteSpace(frameId) ? DefaultFrameId : frameId.Trim();

            if (resolvedName == PlayerName &&
                resolvedAvatarId == AvatarId &&
                resolvedFrameId == FrameId)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                GoldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                resolvedName,
                resolvedAvatarId,
                resolvedFrameId);
        }

        public PlayerProfile WithGoldPassState(GoldPassState goldPassState)
        {
            if (ReferenceEquals(GoldPassState, goldPassState))
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides,
                HeartState,
                goldPassState,
                AdsRemoved,
                UnlimitedHeartEndUnixSeconds,
                FirstTryWins,
                HasFailedCurrentLevel,
                CreatedAtUnixSeconds,
                PlayerName,
                AvatarId,
                FrameId);
        }

        private static ReadOnlyDictionary<BoosterType, int> CopyBoosterCounts(
            IReadOnlyDictionary<BoosterType, int> boosterCounts)
        {
            if (boosterCounts == null)
            {
                throw new ArgumentNullException(nameof(boosterCounts));
            }

            Dictionary<BoosterType, int> copiedCounts = new();

            foreach (KeyValuePair<BoosterType, int> boosterCount in boosterCounts)
            {
                ValidateBoosterType(boosterCount.Key, nameof(boosterCounts));

                if (boosterCount.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(boosterCounts),
                        boosterCount.Value,
                        "Booster count cannot be negative.");
                }

                copiedCounts.Add(boosterCount.Key, boosterCount.Value);
            }

            return new ReadOnlyDictionary<BoosterType, int>(copiedCounts);
        }

        private static HashSet<BoosterType> CopySeenBoosterGuides(
            IReadOnlyCollection<BoosterType> seenBoosterGuides)
        {
            if (seenBoosterGuides == null)
            {
                throw new ArgumentNullException(nameof(seenBoosterGuides));
            }

            HashSet<BoosterType> copiedGuides = new();

            foreach (BoosterType boosterType in seenBoosterGuides)
            {
                ValidateBoosterType(boosterType, nameof(seenBoosterGuides));

                if (!copiedGuides.Add(boosterType))
                {
                    throw new ArgumentException(
                        $"Booster guide {boosterType} is duplicated.",
                        nameof(seenBoosterGuides));
                }
            }

            return copiedGuides;
        }

        private static void ValidateBoosterType(
            BoosterType boosterType,
            string parameterName)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    boosterType,
                    "Booster type is not defined.");
            }
        }
    }
}
