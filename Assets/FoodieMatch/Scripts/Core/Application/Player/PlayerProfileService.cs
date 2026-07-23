using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Domain.Booster;
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

        private Task _saveQueue = Task.CompletedTask;
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

        public HeartState RefreshHeartState()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
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
                HeartState updatedHeartState = GetRefreshedHeartState(
                    currentProfile,
                    currentUtc);

                QueueProfileChange(
                    currentProfile.WithHeartState(updatedHeartState));

                TimeSpan timeUntilNextHeart =
                    updatedHeartState.GetTimeUntilNextHeart(
                        _heartConfig.HeartRecoveryDuration,
                        currentUtc);

                return new HeartStatus(
                    updatedHeartState.HeartCount,
                    _heartConfig.MaxHeartCount,
                    timeUntilNextHeart,
                    _heartConfig.HeartRecoveryDuration);
            }
        }

        public bool HasAvailableHeart()
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
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
                PlayerProfile updatedProfile = currentProfile
                    .WithCurrentLevelNumber(currentLevelNumber)
                    .WithCoinBalance(updatedCoinBalance);

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

        public bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                return _profileSession.CurrentRecord.Profile
                    .HasSeenBoosterGuide(boosterType);
            }
        }

        public void MarkBoosterGuideSeen(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                PlayerProfile currentProfile =
                    _profileSession.CurrentRecord.Profile;
                QueueProfileChange(
                    currentProfile.WithSeenBoosterGuide(boosterType));
            }
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

        private void QueueProfileChange(PlayerProfile updatedProfile)
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

                return;
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
        }

        private async Task SaveAfterAsync(
            Task previousSave,
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
                    return;
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
            }
            catch (Exception exception)
            {
                RaiseSaveFailed(exception.Message);
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
