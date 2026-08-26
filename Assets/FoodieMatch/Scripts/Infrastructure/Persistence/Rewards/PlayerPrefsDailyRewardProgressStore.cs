using System.Globalization;
using FoodieMatch.Core.Application.Rewards;
using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Persistence.Rewards
{
    public sealed class PlayerPrefsDailyRewardProgressStore :
        IDailyRewardProgressStore
    {
        private const string Prefix = "DailyReward.";

        private readonly ISaveService _saveService;

        public PlayerPrefsDailyRewardProgressStore(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public DailyRewardProgress Load()
        {
            string dayValue = _saveService.GetString(
                Prefix + "DayNumber",
                "-1");
            if (!long.TryParse(
                    dayValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long dayNumber) ||
                dayNumber < -1)
            {
                dayNumber = -1;
            }

            int adRewardsClaimed = Clamp(
                _saveService.GetInt(Prefix + "AdRewardsClaimed", 0),
                0,
                DailyRewardService.AdRewardCount);

            string giftAvailableValue = _saveService.GetString(
                Prefix + "DailyGiftAvailableAt",
                "0");
            if (!long.TryParse(
                    giftAvailableValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long giftAvailableAt) ||
                giftAvailableAt < 0)
            {
                giftAvailableAt = 0;
            }

            return new DailyRewardProgress(
                dayNumber,
                NonNegative(Prefix + "CompletedLevels"),
                NonNegative(Prefix + "StorageUses"),
                NonNegative(Prefix + "SwapUses"),
                NonNegative(Prefix + "PlateUses"),
                NonNegative(Prefix + "FridgeUses"),
                NonNegative(Prefix + "ClaimedQuestMask"),
                giftAvailableAt,
                adRewardsClaimed,
                _saveService.GetInt(Prefix + "FinalBonusClaimed", 0) == 1);
        }

        public void Save(DailyRewardProgress progress)
        {
            _saveService.SetString(
                Prefix + "DayNumber",
                progress.DayNumber.ToString(CultureInfo.InvariantCulture));
            _saveService.SetInt(Prefix + "CompletedLevels", progress.CompletedLevels);
            _saveService.SetInt(Prefix + "StorageUses", progress.StorageUses);
            _saveService.SetInt(Prefix + "SwapUses", progress.SwapUses);
            _saveService.SetInt(Prefix + "PlateUses", progress.PlateUses);
            _saveService.SetInt(Prefix + "FridgeUses", progress.FridgeUses);
            _saveService.SetInt(Prefix + "ClaimedQuestMask", progress.ClaimedQuestMask);
            _saveService.SetString(
                Prefix + "DailyGiftAvailableAt",
                progress.DailyGiftAvailableAtUnixSeconds.ToString(
                    CultureInfo.InvariantCulture));
            _saveService.SetInt(Prefix + "AdRewardsClaimed", progress.AdRewardsClaimed);
            _saveService.SetInt(
                Prefix + "FinalBonusClaimed",
                progress.FinalBonusClaimed ? 1 : 0);
            _saveService.Save();
        }

        private int NonNegative(string key)
        {
            return System.Math.Max(0, _saveService.GetInt(key, 0));
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return System.Math.Max(minimum, System.Math.Min(value, maximum));
        }
    }
}
