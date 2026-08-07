using System;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Booster
{
    public sealed class BoosterManager
    {
        private static readonly BoosterType[] AllTypes =
            (BoosterType[])Enum.GetValues(typeof(BoosterType));

        private readonly PlayerProfileService _playerProfileService;
        private readonly IGameBoosterConfig _boosterConfig;

        public BoosterManager(
            PlayerProfileService playerProfileService,
            IGameBoosterConfig boosterConfig)
        {
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
            _boosterConfig = boosterConfig ??
                throw new ArgumentNullException(nameof(boosterConfig));
        }

        public int GetCount(BoosterType type)
        {
            return _playerProfileService.GetBoosterCount(type);
        }

        public int[] GetCounts()
        {
            int[] counts = new int[AllTypes.Length];
            for (int i = 0; i < AllTypes.Length; i++)
            {
                counts[i] = GetCount(AllTypes[i]);
            }
            return counts;
        }

        public void Add(BoosterType type, int amount)
        {
            _playerProfileService.AddBooster(type, amount);
        }

        public bool TryPurchase(BoosterType type, long coinPrice)
        {
            return _playerProfileService.TryPurchaseBooster(type, coinPrice);
        }

        public bool TryUse(BoosterType type)
        {
            return _playerProfileService.TryUseBooster(type);
        }

        public bool HasCount(BoosterType type)
        {
            return GetCount(type) > 0;
        }

        public bool HasSeenGuide(BoosterType type)
        {
            return _playerProfileService.HasSeenBoosterGuide(type);
        }

        public bool TryClaimUnlockReward(BoosterType type)
        {
            return _playerProfileService.TryClaimBoosterUnlockReward(
                type,
                _boosterConfig.UnlockRewardAmount);
        }

        public bool TryMarkGuideSeen(BoosterType type)
        {
            return _playerProfileService.TryMarkBoosterGuideSeen(type);
        }
    }
}
