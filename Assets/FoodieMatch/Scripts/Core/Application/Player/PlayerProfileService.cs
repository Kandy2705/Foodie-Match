using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.GoldPass;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.GoldPass;
using FoodieMatch.Core.Domain.Heart;
using FoodieMatch.Core.Domain.Player;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileService
    {
        private readonly object _stateLock = new();
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly PlayerProfileSession _profileSession;
        private readonly IGameHeartConfig _heartConfig;
        private readonly IClock _clock;

        private Task<bool> _saveQueue = Task.FromResult(true);
        private long _currentChangeVersion;
        private long _savedChangeVersion;

        public PlayerProfileService(
            IPlayerProfileRepository profileRepository,
            PlayerProfileSession profileSession,
            IGameHeartConfig heartConfig,
            IClock clock)
        {
            _profileRepository = profileRepository ??
                throw new ArgumentNullException(nameof(profileRepository));
            _profileSession = profileSession ??
                throw new ArgumentNullException(nameof(profileSession));
            _heartConfig = heartConfig ??
                throw new ArgumentNullException(nameof(heartConfig));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action<string> SaveFailed;

        public int CurrentLevelNumber
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.CurrentLevelNumber;
                }
            }
        }

        public long CoinBalance
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.CoinBalance;
                }
            }
        }

        public bool AdsRemoved
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.AdsRemoved;
                }
            }
        }

        public int FirstTryWins
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.FirstTryWins;
                }
            }
        }

        public long CreatedAtUnixSeconds
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.CreatedAtUnixSeconds;
                }
            }
        }

        public string PlayerName
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.PlayerName;
                }
            }
        }

        public string AvatarId
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.AvatarId;
                }
            }
        }

        public string FrameId
        {
            get
            {
                lock (_stateLock)
                {
                    return _profileSession.CurrentRecord.Profile.FrameId;
                }
            }
        }

        public HeartState RefreshHeartState()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                if (HasUnlimitedHearts(currentProfile, _clock.UtcNow))
                {
                    return currentProfile.HeartState;
                }

                HeartState updatedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    _clock.UtcNow);

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));
                return updatedHeartState;
            }
        }

        public HeartStatus GetHeartStatus()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                DateTimeOffset currentUtc = _clock.UtcNow;
                if (HasUnlimitedHearts(currentProfile, currentUtc))
                {
                    return CreateHeartStatus(
                        currentProfile.HeartState,
                        currentUtc,
                        currentProfile.UnlimitedHeartEndUnixSeconds);
                }

                HeartState updatedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    currentUtc);

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));

                return CreateHeartStatus(updatedHeartState, currentUtc, 0);
            }
        }

        public bool HasAvailableHeart()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                if (HasUnlimitedHearts(currentProfile, _clock.UtcNow))
                {
                    return true;
                }

                HeartState updatedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    _clock.UtcNow);

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));
                return updatedHeartState.HeartCount > 0;
            }
        }

        public bool TrySpendHeart()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                DateTimeOffset currentUtc = _clock.UtcNow;
                if (HasUnlimitedHearts(currentProfile, currentUtc))
                {
                    return true;
                }

                HeartState refreshedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    currentUtc);

                if (!refreshedHeartState.TrySpendHeart(
                        _heartConfig.MaxHeartCount,
                        currentUtc,
                        out HeartState updatedHeartState))
                {
                    QueueProfileChange(
                        currentProfile.WithHeartState(
                            refreshedHeartState));
                    return false;
                }

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));
                return true;
            }
        }

        public void SetCurrentLevelNumber(int levelNumber)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                QueueProfileChange(
                    currentProfile.WithCurrentLevelNumber(levelNumber));
            }
        }

        public void AddCoins(long amount)
        {
            ValidatePositiveCoinAmount(amount, nameof(amount));

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                long updatedCoinBalance = checked(
                    currentProfile.CoinBalance + amount);
                QueueProfileChange(
                    currentProfile.WithCoinBalance(updatedCoinBalance));
            }
        }

        public void ApplyLevelCompletionReward(
            int currentLevelNumber,
            long coinReward)
        {
            ValidatePositiveCoinAmount(coinReward, nameof(coinReward));

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                long updatedCoinBalance = checked(
                    currentProfile.CoinBalance + coinReward);
                int updatedFirstTryWins = !currentProfile.HasFailedCurrentLevel
                    ? checked(currentProfile.FirstTryWins + 1)
                    : currentProfile.FirstTryWins;
                PlayerProfile updatedProfile = currentProfile
                    .WithCurrentLevelNumber(currentLevelNumber)
                    .WithCoinBalance(updatedCoinBalance)
                    .WithFirstTryWins(updatedFirstTryWins)
                    .WithResetFailedCurrentLevel();

                QueueProfileChange(updatedProfile);
            }
        }

        public void RecordCurrentLevelFailed()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                if (currentProfile.HasFailedCurrentLevel)
                {
                    return;
                }

                QueueProfileChange(currentProfile.WithFailedCurrentLevel());
            }
        }

        public void UpdateCustomization(
            string playerName,
            string avatarId,
            string frameId)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                PlayerProfile updatedProfile = currentProfile.WithCustomization(
                    playerName,
                    avatarId,
                    frameId);

                if (ReferenceEquals(currentProfile, updatedProfile))
                {
                    return;
                }

                QueueProfileChange(updatedProfile);
            }
        }

        public bool TrySpendCoins(long amount)
        {
            ValidatePositiveCoinAmount(amount, nameof(amount));

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;

                if (currentProfile.CoinBalance < amount)
                {
                    return false;
                }

                QueueProfileChange(
                    currentProfile.WithCoinBalance(
                        currentProfile.CoinBalance - amount));
                return true;
            }
        }

        public bool TryFillHeartsWithCoins(long coinCost)
        {
            ValidatePositiveCoinAmount(coinCost, nameof(coinCost));

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                DateTimeOffset currentUtc = _clock.UtcNow;

                if (HasUnlimitedHearts(currentProfile, currentUtc))
                {
                    return false;
                }

                HeartState refreshedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    currentUtc);

                if (refreshedHeartState.HeartCount >=
                        _heartConfig.MaxHeartCount ||
                    currentProfile.CoinBalance < coinCost)
                {
                    QueueProfileChange(
                        currentProfile.WithHeartState(
                            refreshedHeartState));
                    return false;
                }

                HeartState fullHeartState = new(
                    _heartConfig.MaxHeartCount,
                    recoveryStartedAtUtc: null);
                PlayerProfile updatedProfile = currentProfile
                    .WithCoinBalance(currentProfile.CoinBalance - coinCost)
                    .WithHeartState(fullHeartState);

                QueueProfileChange(updatedProfile);
                return true;
            }
        }

        public bool TryAddHeart()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                DateTimeOffset currentUtc = _clock.UtcNow;

                if (HasUnlimitedHearts(currentProfile, currentUtc))
                {
                    return false;
                }

                HeartState refreshedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    currentUtc);

                if (!refreshedHeartState.TryAddHeart(
                        _heartConfig.MaxHeartCount,
                        currentUtc,
                        out HeartState updatedHeartState))
                {
                    QueueProfileChange(
                        currentProfile.WithHeartState(
                            refreshedHeartState));
                    return false;
                }

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));
                return true;
            }
        }

        public bool TryPurchaseBooster(
            BoosterType boosterType,
            long coinCost)
        {
            ValidatePositiveCoinAmount(coinCost, nameof(coinCost));

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                int currentBoosterCount =
                    currentProfile.GetBoosterCount(boosterType);

                if (currentProfile.CoinBalance < coinCost)
                {
                    return false;
                }

                int updatedBoosterCount = checked(currentBoosterCount + 1);
                PlayerProfile updatedProfile = currentProfile
                    .WithCoinBalance(currentProfile.CoinBalance - coinCost)
                    .WithBoosterCount(boosterType, updatedBoosterCount);

                QueueProfileChange(updatedProfile);
                return true;
            }
        }

        public int GetBoosterCount(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                return _profileSession.CurrentRecord.Profile
                    .GetBoosterCount(boosterType);
            }
        }

        public bool TryUseBooster(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                int currentCount =
                    currentProfile.GetBoosterCount(boosterType);

                if (currentCount <= 0)
                {
                    return false;
                }

                QueueProfileChange(
                    currentProfile.WithBoosterCount(
                        boosterType,
                        currentCount - 1));
                return true;
            }
        }

        public void AddBooster(BoosterType boosterType, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Booster amount must be greater than zero.");
            }

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                int updatedCount = checked(
                    currentProfile.GetBoosterCount(boosterType) + amount);
                QueueProfileChange(
                    currentProfile.WithBoosterCount(
                        boosterType,
                        updatedCount));
            }
        }

        public void ApplyDebugUpdate(PlayerProfileDebugUpdate update)
        {
            if (update.HeartCount > _heartConfig.MaxHeartCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(update),
                    update.HeartCount,
                    "Heart count cannot exceed the configured maximum.");
            }

            lock (_stateLock)
            {
                DateTimeOffset? recoveryStartedAtUtc =
                    update.HeartCount < _heartConfig.MaxHeartCount
                        ? _clock.UtcNow
                        : null;

                HeartState heartState = new(
                    update.HeartCount,
                    recoveryStartedAtUtc);

                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                PlayerProfile updatedProfile = currentProfile
                    .WithCurrentLevelNumber(update.CurrentLevelNumber)
                    .WithCoinBalance(update.CoinBalance)
                    .WithHeartState(heartState)
                    .WithBoosterCount(
                        BoosterType.Plate,
                        update.PlateBoosterCount)
                    .WithBoosterCount(
                        BoosterType.Storage,
                        update.StorageBoosterCount)
                    .WithBoosterCount(
                        BoosterType.Swap,
                        update.SwapBoosterCount)
                    .WithBoosterCount(
                        BoosterType.Fridge,
                        update.FridgeBoosterCount)
                    .WithAdsRemoved(update.AdsRemoved);

                QueueProfileChange(updatedProfile);
            }
        }

        public bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                return _profileSession.CurrentRecord.Profile
                    .HasSeenBoosterGuide(boosterType);
            }
        }

        public bool TryClaimBoosterUnlockReward(
            BoosterType boosterType,
            int rewardAmount)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;

                if (currentProfile.HasSeenBoosterGuide(boosterType))
                {
                    return false;
                }

                int updatedBoosterCount = checked(
                    currentProfile.GetBoosterCount(boosterType) + rewardAmount);
                PlayerProfile updatedProfile = currentProfile
                    .WithBoosterCount(boosterType, updatedBoosterCount)
                    .WithSeenBoosterGuide(boosterType);

                QueueProfileChange(updatedProfile);
                return true;
            }
        }

        public bool TryMarkBoosterGuideSeen(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;

                if (currentProfile.HasSeenBoosterGuide(boosterType))
                {
                    return false;
                }

                QueueProfileChange(
                    currentProfile.WithSeenBoosterGuide(boosterType));
                return true;
            }
        }

        public GoldPassState RefreshGoldPassSeason(string seasonId)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState currentState = currentProfile.GoldPassState;

                if (currentState.SeasonId == seasonId)
                {
                    return currentState;
                }

                GoldPassState updatedState =
                    GoldPassState.CreateForSeason(seasonId);
                QueueProfileChange(
                    currentProfile.WithGoldPassState(updatedState));
                return updatedState;
            }
        }

        public void AddGoldPassSpoons(string seasonId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState currentState = GetGoldPassStateForSeason(
                    currentProfile,
                    seasonId);
                QueueProfileChange(
                    currentProfile.WithGoldPassState(
                        currentState.WithAddedSpoons(amount)));
            }
        }

        public Task<bool> TryActivateGoldPassSeasonPassAsync(string seasonId)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState currentState = GetGoldPassStateForSeason(
                    currentProfile,
                    seasonId);

                if (currentState.IsSeasonPassPurchased)
                {
                    return Task.FromResult(false);
                }

                return QueueProfileChange(
                    currentProfile.WithGoldPassState(
                        currentState.WithSeasonPassPurchased()));
            }
        }

        public void ApplyGoldPassDebugUpdate(
            string seasonId,
            int spoonCount,
            bool isSeasonPassPurchased)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState updatedState = GetGoldPassStateForSeason(
                        currentProfile,
                        seasonId)
                    .WithSpoonCount(spoonCount)
                    .WithSeasonPassPurchaseStatus(isSeasonPassPurchased);

                QueueProfileChange(
                    currentProfile.WithGoldPassState(updatedState));
            }
        }

        public void ResetGoldPassClaimHistory(string seasonId)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState updatedState = GetGoldPassStateForSeason(
                        currentProfile,
                        seasonId)
                    .WithoutClaimedMilestones();

                QueueProfileChange(
                    currentProfile.WithGoldPassState(updatedState));
            }
        }

        public GoldPassClaimResult TryClaimGoldPassReward(
            string seasonId,
            GoldPassMilestoneDefinition milestone,
            GoldPassTrack track)
        {
            if (!Enum.IsDefined(typeof(GoldPassTrack), track))
            {
                throw new ArgumentOutOfRangeException(nameof(track));
            }

            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState currentState = GetGoldPassStateForSeason(
                    currentProfile,
                    seasonId);
                bool seasonChanged = !ReferenceEquals(
                    currentProfile.GoldPassState,
                    currentState);

                if (seasonChanged)
                {
                    currentProfile = currentProfile.WithGoldPassState(
                        currentState);
                }

                if (currentState.SpoonCount < milestone.RequiredSpoons)
                {
                    return CompleteGoldPassClaimFailure(
                        currentProfile,
                        seasonChanged,
                        GoldPassClaimResult.MilestoneLocked);
                }

                if (track == GoldPassTrack.Season &&
                    !currentState.IsSeasonPassPurchased)
                {
                    return CompleteGoldPassClaimFailure(
                        currentProfile,
                        seasonChanged,
                        GoldPassClaimResult.SeasonPassRequired);
                }

                bool alreadyClaimed = track == GoldPassTrack.Free
                    ? currentState.HasClaimedFreeMilestone(milestone.Level)
                    : currentState.HasClaimedSeasonMilestone(milestone.Level);

                if (alreadyClaimed)
                {
                    return CompleteGoldPassClaimFailure(
                        currentProfile,
                        seasonChanged,
                        GoldPassClaimResult.AlreadyClaimed);
                }

                GoldPassRewardDefinition reward = track == GoldPassTrack.Free
                    ? milestone.FreeReward
                    : milestone.SeasonReward;
                GoldPassState updatedState = track == GoldPassTrack.Free
                    ? currentState.WithClaimedFreeMilestone(milestone.Level)
                    : currentState.WithClaimedSeasonMilestone(milestone.Level);
                PlayerProfile updatedProfile = ApplyGoldPassReward(
                    currentProfile,
                    reward,
                    updatedState);

                QueueProfileChange(updatedProfile);
                return GoldPassClaimResult.Succeeded;
            }
        }

        public bool TryClaimAllGoldPassRewards(
            string seasonId,
            IReadOnlyList<GoldPassMilestoneDefinition> milestones)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                GoldPassState currentState = GetGoldPassStateForSeason(
                    currentProfile,
                    seasonId);
                GoldPassState updatedState = currentState;
                List<GoldPassRewardDefinition> rewards = new();

                for (int i = 0; i < milestones.Count; i++)
                {
                    GoldPassMilestoneDefinition milestone = milestones[i];

                    if (currentState.SpoonCount < milestone.RequiredSpoons)
                    {
                        continue;
                    }

                    if (!currentState.HasClaimedFreeMilestone(milestone.Level))
                    {
                        rewards.Add(milestone.FreeReward);
                        updatedState = updatedState.WithClaimedFreeMilestone(
                            milestone.Level);
                    }

                    if (currentState.IsSeasonPassPurchased &&
                        !currentState.HasClaimedSeasonMilestone(milestone.Level))
                    {
                        rewards.Add(milestone.SeasonReward);
                        updatedState = updatedState.WithClaimedSeasonMilestone(
                            milestone.Level);
                    }
                }

                if (rewards.Count == 0)
                {
                    return false;
                }

                PlayerProfile updatedProfile = currentProfile;

                for (int i = 0; i < rewards.Count; i++)
                {
                    updatedProfile = ApplyGoldPassReward(
                        updatedProfile,
                        rewards[i],
                        updatedState);
                }

                QueueProfileChange(updatedProfile);
                return true;
            }
        }

        public async Task<ShopRewardApplyResult> ApplyShopRewardsAsync(
            ShopRewardDefinition rewards)
        {
            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            PlayerProfile updatedProfile;
            Task<bool> saveTask;

            lock (_stateLock)
            {
                PlayerProfile currentProfile = _profileSession.CurrentRecord.Profile;
                DateTimeOffset currentUtc = _clock.UtcNow;
                long currentUnixSeconds = currentUtc.ToUnixTimeSeconds();
                long updatedCoinBalance = checked(
                    currentProfile.CoinBalance + rewards.Coins);
                Dictionary<BoosterType, int> updatedBoosterCounts = new(
                    currentProfile.BoosterCounts);

                foreach (KeyValuePair<BoosterType, int> boosterReward in rewards.BoosterAmounts)
                {
                    updatedBoosterCounts[boosterReward.Key] = checked(
                        currentProfile.GetBoosterCount(boosterReward.Key) +
                        boosterReward.Value);
                }

                HeartState updatedHeartState =
                    HasUnlimitedHearts(currentProfile, currentUtc)
                        ? currentProfile.HeartState
                        : GetRefreshedHeartState(currentProfile, currentUtc);
                long updatedUnlimitedHeartEnd =
                    currentProfile.UnlimitedHeartEndUnixSeconds;

                if (rewards.UnlimitedHeartSeconds > 0)
                {
                    updatedHeartState = ShiftHeartRecovery(
                        updatedHeartState,
                        rewards.UnlimitedHeartSeconds);
                    long baseEnd = Math.Max(
                        currentUnixSeconds,
                        currentProfile.UnlimitedHeartEndUnixSeconds);
                    updatedUnlimitedHeartEnd = checked(
                        baseEnd + rewards.UnlimitedHeartSeconds);
                }

                updatedProfile = currentProfile.WithResourceState(
                    updatedCoinBalance,
                    updatedBoosterCounts,
                    updatedHeartState,
                    currentProfile.AdsRemoved || rewards.RemoveAds,
                    updatedUnlimitedHeartEnd);
                saveTask = QueueProfileChange(updatedProfile);
            }

            if (!await saveTask)
            {
                throw new InvalidOperationException(
                    "Shop rewards could not be saved to the player profile.");
            }

            return new ShopRewardApplyResult(
                updatedProfile.CoinBalance,
                updatedProfile.HeartState.HeartCount,
                updatedProfile.UnlimitedHeartEndUnixSeconds,
                updatedProfile.AdsRemoved,
                updatedProfile.BoosterCounts);
        }

        private PlayerProfile ApplyGoldPassReward(
            PlayerProfile currentProfile,
            GoldPassRewardDefinition reward,
            GoldPassState updatedGoldPassState)
        {
            long coinReward = 0;
            long unlimitedHeartSeconds = 0;
            Dictionary<BoosterType, int> boosterRewards = new();
            CollectGoldPassReward(
                reward,
                ref coinReward,
                ref unlimitedHeartSeconds,
                boosterRewards);

            long updatedCoinBalance = checked(
                currentProfile.CoinBalance + coinReward);
            Dictionary<BoosterType, int> updatedBoosterCounts = new(
                currentProfile.BoosterCounts);

            foreach (KeyValuePair<BoosterType, int> boosterReward in boosterRewards)
            {
                updatedBoosterCounts[boosterReward.Key] = checked(
                    currentProfile.GetBoosterCount(boosterReward.Key) +
                    boosterReward.Value);
            }

            DateTimeOffset currentUtc = _clock.UtcNow;
            HeartState updatedHeartState =
                HasUnlimitedHearts(currentProfile, currentUtc)
                    ? currentProfile.HeartState
                    : GetRefreshedHeartState(currentProfile, currentUtc);
            long updatedUnlimitedHeartEnd =
                currentProfile.UnlimitedHeartEndUnixSeconds;

            if (unlimitedHeartSeconds > 0)
            {
                updatedHeartState = ShiftHeartRecovery(
                    updatedHeartState,
                    unlimitedHeartSeconds);
                long baseEnd = Math.Max(
                    currentUtc.ToUnixTimeSeconds(),
                    currentProfile.UnlimitedHeartEndUnixSeconds);
                updatedUnlimitedHeartEnd = checked(
                    baseEnd + unlimitedHeartSeconds);
            }

            return currentProfile
                .WithResourceState(
                    updatedCoinBalance,
                    updatedBoosterCounts,
                    updatedHeartState,
                    currentProfile.AdsRemoved,
                    updatedUnlimitedHeartEnd)
                .WithGoldPassState(updatedGoldPassState);
        }

        private static void CollectGoldPassReward(
            GoldPassRewardDefinition reward,
            ref long coinReward,
            ref long unlimitedHeartSeconds,
            Dictionary<BoosterType, int> boosterRewards)
        {
            switch (reward.Type)
            {
                case GoldPassRewardType.Coin:
                    coinReward = checked(coinReward + reward.Amount);
                    return;

                case GoldPassRewardType.UnlimitedHeart:
                    unlimitedHeartSeconds = checked(
                        unlimitedHeartSeconds + reward.UnlimitedHeartSeconds);
                    return;

                case GoldPassRewardType.Booster:
                    BoosterType boosterType = reward.BoosterType.Value;
                    int currentAmount = boosterRewards.TryGetValue(
                        boosterType,
                        out int amount)
                            ? amount
                            : 0;
                    boosterRewards[boosterType] = checked(
                        currentAmount + (int)reward.Amount);
                    return;

                case GoldPassRewardType.Treasure1:
                case GoldPassRewardType.Treasure2:
                case GoldPassRewardType.Treasure3:
                    for (int i = 0; i < reward.Contents.Count; i++)
                    {
                        CollectGoldPassReward(
                            reward.Contents[i],
                            ref coinReward,
                            ref unlimitedHeartSeconds,
                            boosterRewards);
                    }

                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reward));
            }
        }

        private static GoldPassState GetGoldPassStateForSeason(
            PlayerProfile profile,
            string seasonId)
        {
            return profile.GoldPassState.SeasonId == seasonId
                ? profile.GoldPassState
                : GoldPassState.CreateForSeason(seasonId);
        }

        private GoldPassClaimResult CompleteGoldPassClaimFailure(
            PlayerProfile currentProfile,
            bool seasonChanged,
            GoldPassClaimResult result)
        {
            if (seasonChanged)
            {
                QueueProfileChange(currentProfile);
            }

            return result;
        }

        private HeartState GetRefreshedHeartState(
            PlayerProfile profile,
            DateTimeOffset utcNow)
        {
            return profile.HeartState.RefreshRecovery(
                _heartConfig.MaxHeartCount,
                _heartConfig.HeartRecoveryDuration,
                utcNow);
        }

        private HeartStatus CreateHeartStatus(
            HeartState heartState,
            DateTimeOffset currentUtc,
            long unlimitedHeartEndUnixSeconds)
        {
            DateTimeOffset? unlimitedHeartEndUtc =
                unlimitedHeartEndUnixSeconds > currentUtc.ToUnixTimeSeconds()
                    ? DateTimeOffset.FromUnixTimeSeconds(unlimitedHeartEndUnixSeconds)
                    : null;

            DateTimeOffset recoveryReferenceUtc =
                unlimitedHeartEndUtc ?? currentUtc;
            TimeSpan timeUntilNextHeart =
                heartState.GetTimeUntilNextHeart(
                    _heartConfig.HeartRecoveryDuration,
                    recoveryReferenceUtc);

            return new HeartStatus(
                heartState.HeartCount,
                _heartConfig.MaxHeartCount,
                timeUntilNextHeart,
                _heartConfig.HeartRecoveryDuration,
                unlimitedHeartEndUtc);
        }

        private static HeartState ShiftHeartRecovery(
            HeartState heartState,
            long durationSeconds)
        {
            if (!heartState.RecoveryStartedAtUtc.HasValue)
            {
                return heartState;
            }

            return new HeartState(
                heartState.HeartCount,
                heartState.RecoveryStartedAtUtc.Value.AddSeconds(durationSeconds));
        }

        private static bool HasUnlimitedHearts(
            PlayerProfile profile,
            DateTimeOffset currentUtc)
        {
            return profile.UnlimitedHeartEndUnixSeconds >
                   currentUtc.ToUnixTimeSeconds();
        }

        private Task<bool> QueueProfileChange(PlayerProfile updatedProfile)
        {
            PlayerProfileRecord currentRecord = _profileSession.CurrentRecord;

            if (ReferenceEquals(currentRecord.Profile, updatedProfile))
            {
                if (_savedChangeVersion < _currentChangeVersion &&
                    _saveQueue.IsCompleted)
                {
                    _saveQueue = SaveAfterAsync(
                        _saveQueue,
                        updatedProfile,
                        _currentChangeVersion);
                }

                return _saveQueue;
            }

            _currentChangeVersion++;
            long changeVersion = _currentChangeVersion;
            _profileSession.ReplaceCurrentRecord(
                new PlayerProfileRecord(
                    updatedProfile,
                    currentRecord.Revision));
            _saveQueue = SaveAfterAsync(
                _saveQueue,
                updatedProfile,
                changeVersion);
            return _saveQueue;
        }

        private async Task<bool> SaveAfterAsync(
            Task<bool> previousSave,
            PlayerProfile profile,
            long changeVersion)
        {
            try
            {
                await previousSave;
            }
            catch (Exception exception)
            {
                RaiseSaveFailed(exception.Message);
            }

            try
            {
                long expectedRevision;

                lock (_stateLock)
                {
                    expectedRevision = _profileSession.CurrentRecord.Revision;
                }

                PlayerProfileSaveResult saveResult =
                    await _profileRepository.SaveAsync(
                        profile,
                        expectedRevision);

                if (!saveResult.IsSuccess)
                {
                    RaiseSaveFailed(CreateSaveErrorMessage(saveResult));
                    return false;
                }

                lock (_stateLock)
                {
                    PlayerProfile latestProfile =
                        _profileSession.CurrentRecord.Profile;
                    _profileSession.ReplaceCurrentRecord(
                        new PlayerProfileRecord(
                            latestProfile,
                            saveResult.Record.Revision));
                    _savedChangeVersion = Math.Max(
                        _savedChangeVersion,
                        changeVersion);
                }

                return true;
            }
            catch (Exception exception)
            {
                RaiseSaveFailed(exception.Message);
                return false;
            }
        }

        private void RaiseSaveFailed(string errorMessage)
        {
            SaveFailed?.Invoke(errorMessage);
        }

        private static void ValidatePositiveCoinAmount(
            long amount,
            string parameterName)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    amount,
                    "Coin amount must be greater than zero.");
            }
        }

        private static string CreateSaveErrorMessage(
            PlayerProfileSaveResult saveResult)
        {
            if (saveResult.Status == PlayerProfileSaveStatus.Conflict)
            {
                return $"Player profile save conflicted with revision " +
                       $"{saveResult.CurrentRevision}.";
            }

            return saveResult.ErrorMessage;
        }
    }
}
