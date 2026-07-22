using System;
using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.Data.Booster
{
    public sealed class BoosterManager
    {
        private static readonly BoosterType[] AllTypes =
            (BoosterType[])Enum.GetValues(typeof(BoosterType));

        private readonly PlayerProfileService _playerProfileService;

        public BoosterManager(PlayerProfileService playerProfileService)
        {
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
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

        public void MarkGuideSeen(BoosterType type)
        {
            _playerProfileService.MarkBoosterGuideSeen(type);
        }
    }
}
