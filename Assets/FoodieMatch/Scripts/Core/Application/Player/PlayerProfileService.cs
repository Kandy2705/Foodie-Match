using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.Data.Booster;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileService
    {
        private readonly object _stateLock = new();
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly PlayerProfileSession _profileSession;

        private Task _saveQueue = Task.CompletedTask;
        private long _currentChangeVersion;
        private long _savedChangeVersion;

        public PlayerProfileService(
            IPlayerProfileRepository profileRepository,
            PlayerProfileSession profileSession)
        {
            _profileRepository = profileRepository ??
                throw new ArgumentNullException(nameof(profileRepository));
            _profileSession = profileSession ??
                throw new ArgumentNullException(nameof(profileSession));
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
