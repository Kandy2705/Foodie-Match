using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Rewards
{
    public sealed class DailyRewardService
    {
        public const int DailyGiftCoinAmount = 50;
        public const int QuestCoinAmount = 40;
        public const int FirstAdCoinAmount = 100;
        public const int FinalBonusCoinAmount = 300;
        public const int AdRewardCount = 3;
        public const int DailyGiftCooldownSeconds = 60 * 60;

        private const int CompleteLevelTarget = 3;
        private const int BoosterUseTarget = 2;
        private const long SecondsPerDay = 24 * 60 * 60;

        private readonly object _stateLock = new();
        private readonly IDailyRewardProgressStore _progressStore;
        private readonly PlayerProfileService _playerProfileService;
        private readonly BoosterManager _boosterManager;
        private readonly IClock _clock;

        private DailyRewardProgress _progress;

        public DailyRewardService(
            IDailyRewardProgressStore progressStore,
            PlayerProfileService playerProfileService,
            BoosterManager boosterManager,
            IClock clock)
        {
            _progressStore = progressStore ??
                throw new ArgumentNullException(nameof(progressStore));
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
            _boosterManager = boosterManager ??
                throw new ArgumentNullException(nameof(boosterManager));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _progress = _progressStore.Load() ??
                DailyRewardProgress.CreateEmpty(dayNumber: -1);

            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
            }
        }

        public DailyRewardStatus GetStatus()
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();

                List<DailyQuestStatus> quests = new()
                {
                    CreateQuestStatus(
                        DailyQuestType.CompleteLevels,
                        _progress.CompletedLevels,
                        CompleteLevelTarget),
                    CreateQuestStatus(
                        DailyQuestType.UseStorage,
                        _progress.StorageUses,
                        BoosterUseTarget),
                    CreateQuestStatus(
                        DailyQuestType.UseSwap,
                        _progress.SwapUses,
                        BoosterUseTarget),
                    CreateQuestStatus(
                        DailyQuestType.UsePlate,
                        _progress.PlateUses,
                        BoosterUseTarget),
                    CreateQuestStatus(
                        DailyQuestType.UseFridge,
                        _progress.FridgeUses,
                        BoosterUseTarget)
                };

                DateTimeOffset nowUtc = _clock.UtcNow;
                return new DailyRewardStatus(
                    quests,
                    DateTimeOffset.FromUnixTimeSeconds(
                        _progress.DailyGiftAvailableAtUnixSeconds),
                    nowUtc,
                    _progress.AdRewardsClaimed,
                    _progress.FinalBonusClaimed,
                    DateTimeOffset.FromUnixTimeSeconds(
                        checked((_progress.DayNumber + 1) * SecondsPerDay)));
            }
        }

        public void RecordLevelCompleted()
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
                if (_progress.CompletedLevels >= CompleteLevelTarget)
                {
                    return;
                }

                ReplaceProgress(completedLevels: _progress.CompletedLevels + 1);
            }
        }

        public void RecordBoosterUsed(BoosterType boosterType)
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();

                switch (boosterType)
                {
                    case BoosterType.Storage:
                        if (_progress.StorageUses < BoosterUseTarget)
                        {
                            ReplaceProgress(storageUses: _progress.StorageUses + 1);
                        }
                        return;

                    case BoosterType.Swap:
                        if (_progress.SwapUses < BoosterUseTarget)
                        {
                            ReplaceProgress(swapUses: _progress.SwapUses + 1);
                        }
                        return;

                    case BoosterType.Plate:
                        if (_progress.PlateUses < BoosterUseTarget)
                        {
                            ReplaceProgress(plateUses: _progress.PlateUses + 1);
                        }
                        return;

                    case BoosterType.Fridge:
                        if (_progress.FridgeUses < BoosterUseTarget)
                        {
                            ReplaceProgress(fridgeUses: _progress.FridgeUses + 1);
                        }
                        return;

                    case BoosterType.Box:
                        return;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(boosterType));
                }
            }
        }

        public bool TryClaimQuest(DailyQuestType questType)
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
                int bit = 1 << (int)questType;
                if ((_progress.ClaimedQuestMask & bit) != 0 ||
                    GetQuestProgress(questType) < GetQuestTarget(questType))
                {
                    return false;
                }

                _playerProfileService.AddCoins(QuestCoinAmount);
                ReplaceProgress(claimedQuestMask: _progress.ClaimedQuestMask | bit);
                return true;
            }
        }

        public bool TryClaimDailyGift()
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
                long nowUnixSeconds = _clock.UtcNow.ToUnixTimeSeconds();
                if (_progress.DailyGiftAvailableAtUnixSeconds > nowUnixSeconds)
                {
                    return false;
                }

                _playerProfileService.AddCoins(DailyGiftCoinAmount);
                ReplaceProgress(
                    dailyGiftAvailableAtUnixSeconds:
                        checked(nowUnixSeconds + DailyGiftCooldownSeconds));
                return true;
            }
        }

        public bool CanClaimFreeReward(int rewardIndex)
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
                return rewardIndex >= 0 && rewardIndex <= AdRewardCount &&
                    (rewardIndex < AdRewardCount
                        ? _progress.AdRewardsClaimed == rewardIndex
                        : _progress.AdRewardsClaimed == AdRewardCount &&
                          !_progress.FinalBonusClaimed);
            }
        }

        public bool TryClaimFreeReward(int rewardIndex)
        {
            lock (_stateLock)
            {
                ResetForCurrentDayIfNeeded();
                if (!CanClaimFreeRewardWithoutLock(rewardIndex))
                {
                    return false;
                }

                switch (rewardIndex)
                {
                    case 0:
                        _playerProfileService.AddCoins(FirstAdCoinAmount);
                        ReplaceProgress(adRewardsClaimed: 1);
                        return true;
                    case 1:
                        _boosterManager.Add(BoosterType.Swap, amount: 1);
                        ReplaceProgress(adRewardsClaimed: 2);
                        return true;
                    case 2:
                        _boosterManager.Add(BoosterType.Storage, amount: 1);
                        ReplaceProgress(adRewardsClaimed: 3);
                        return true;
                    case 3:
                        _playerProfileService.AddCoins(FinalBonusCoinAmount);
                        ReplaceProgress(finalBonusClaimed: true);
                        return true;
                    default:
                        return false;
                }
            }
        }

        private DailyQuestStatus CreateQuestStatus(
            DailyQuestType type,
            int progress,
            int target)
        {
            return new DailyQuestStatus(
                type,
                progress,
                target,
                QuestCoinAmount,
                (_progress.ClaimedQuestMask & (1 << (int)type)) != 0);
        }

        private int GetQuestProgress(DailyQuestType type)
        {
            return type switch
            {
                DailyQuestType.CompleteLevels => _progress.CompletedLevels,
                DailyQuestType.UseStorage => _progress.StorageUses,
                DailyQuestType.UseSwap => _progress.SwapUses,
                DailyQuestType.UsePlate => _progress.PlateUses,
                DailyQuestType.UseFridge => _progress.FridgeUses,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static int GetQuestTarget(DailyQuestType type)
        {
            return type == DailyQuestType.CompleteLevels
                ? CompleteLevelTarget
                : BoosterUseTarget;
        }

        private bool CanClaimFreeRewardWithoutLock(int rewardIndex)
        {
            if (rewardIndex < 0 || rewardIndex > AdRewardCount)
            {
                return false;
            }

            return rewardIndex < AdRewardCount
                ? _progress.AdRewardsClaimed == rewardIndex
                : _progress.AdRewardsClaimed == AdRewardCount &&
                  !_progress.FinalBonusClaimed;
        }

        private void ResetForCurrentDayIfNeeded()
        {
            long currentDay = _clock.UtcNow.ToUnixTimeSeconds() / SecondsPerDay;
            if (_progress.DayNumber >= currentDay)
            {
                return;
            }

            _progress = DailyRewardProgress.CreateEmpty(currentDay);
            _progressStore.Save(_progress);
        }

        private void ReplaceProgress(
            int? completedLevels = null,
            int? storageUses = null,
            int? swapUses = null,
            int? plateUses = null,
            int? fridgeUses = null,
            int? claimedQuestMask = null,
            long? dailyGiftAvailableAtUnixSeconds = null,
            int? adRewardsClaimed = null,
            bool? finalBonusClaimed = null)
        {
            _progress = new DailyRewardProgress(
                _progress.DayNumber,
                completedLevels ?? _progress.CompletedLevels,
                storageUses ?? _progress.StorageUses,
                swapUses ?? _progress.SwapUses,
                plateUses ?? _progress.PlateUses,
                fridgeUses ?? _progress.FridgeUses,
                claimedQuestMask ?? _progress.ClaimedQuestMask,
                dailyGiftAvailableAtUnixSeconds ??
                _progress.DailyGiftAvailableAtUnixSeconds,
                adRewardsClaimed ?? _progress.AdRewardsClaimed,
                finalBonusClaimed ?? _progress.FinalBonusClaimed);
            _progressStore.Save(_progress);
        }
    }
}
