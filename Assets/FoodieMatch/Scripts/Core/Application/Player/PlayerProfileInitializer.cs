using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Heart;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.Data.Booster;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileInitializer
    {
        private static readonly BoosterType[] AllBoosterTypes =
            (BoosterType[])Enum.GetValues(typeof(BoosterType));

        private readonly IPlayerProfileRepository _profileRepository;
        private readonly IInvalidPlayerProfileRecovery _invalidProfileRecovery;
        private readonly PlayerProfileSession _profileSession;
        private readonly IGameHeartConfig _heartConfig;

        public PlayerProfileInitializer(
            IPlayerProfileRepository profileRepository,
            IInvalidPlayerProfileRecovery invalidProfileRecovery,
            PlayerProfileSession profileSession,
            IGameHeartConfig heartConfig)
        {
            _profileRepository = profileRepository ??
                throw new ArgumentNullException(nameof(profileRepository));
            _invalidProfileRecovery = invalidProfileRecovery ??
                throw new ArgumentNullException(nameof(invalidProfileRecovery));
            _profileSession = profileSession ??
                throw new ArgumentNullException(nameof(profileSession));
            _heartConfig = heartConfig ??
                throw new ArgumentNullException(nameof(heartConfig));
        }

        public async Task<PlayerProfileInitializationResult> InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            if (_profileSession.IsInitialized)
            {
                return PlayerProfileInitializationResult.Succeeded(
                    _profileSession.CurrentRecord,
                    recoveredInvalidData: false);
            }

            try
            {
                PlayerProfileLoadResult loadResult =
                    await _profileRepository.LoadAsync(cancellationToken);
                bool recoveredInvalidData = false;

                if (loadResult.Status == PlayerProfileLoadStatus.InvalidData)
                {
                    if (!_invalidProfileRecovery.TryBackupAndRemove(out string recoveryError))
                    {
                        return PlayerProfileInitializationResult.Failed(recoveryError);
                    }

                    recoveredInvalidData = true;
                    loadResult = PlayerProfileLoadResult.NotFound();
                }

                if (loadResult.Status == PlayerProfileLoadStatus.UnsupportedVersion ||
                    loadResult.Status == PlayerProfileLoadStatus.Failed)
                {
                    return PlayerProfileInitializationResult.Failed(loadResult.ErrorMessage);
                }

                return await InitializeSessionAsync(
                    loadResult,
                    recoveredInvalidData,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return PlayerProfileInitializationResult.Failed(exception.Message);
            }
        }

        private async Task<PlayerProfileInitializationResult> InitializeSessionAsync(
            PlayerProfileLoadResult loadResult,
            bool recoveredInvalidData,
            CancellationToken cancellationToken)
        {
            if (loadResult.IsSuccess)
            {
                _profileSession.Initialize(loadResult.Record);
                return PlayerProfileInitializationResult.Succeeded(
                    loadResult.Record,
                    recoveredInvalidData);
            }

            PlayerProfileSaveResult saveResult = await _profileRepository.SaveAsync(
                CreateDefaultProfile(),
                expectedRevision: 0,
                cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return PlayerProfileInitializationResult.Failed(
                    CreateSaveErrorMessage(saveResult));
            }

            _profileSession.Initialize(saveResult.Record);
            return PlayerProfileInitializationResult.Succeeded(
                saveResult.Record,
                recoveredInvalidData);
        }

        private PlayerProfile CreateDefaultProfile()
        {
            Dictionary<BoosterType, int> boosterCounts = new();

            foreach (BoosterType boosterType in AllBoosterTypes)
            {
                boosterCounts.Add(boosterType, 0);
            }

            return new PlayerProfile(
                currentLevelNumber: 1,
                coinBalance: 0,
                boosterCounts,
                seenBoosterGuides: Array.Empty<BoosterType>(),
                new HeartState(
                    _heartConfig.MaxHeartCount,
                    recoveryStartedAtUtc: null));
        }

        private static string CreateSaveErrorMessage(PlayerProfileSaveResult saveResult)
        {
            if (saveResult.Status == PlayerProfileSaveStatus.Conflict)
            {
                return $"Player profile changed while it was being initialized. " +
                       $"Current revision is {saveResult.CurrentRevision}.";
            }

            return saveResult.ErrorMessage;
        }
    }
}
