using System;

namespace FoodieMatch.UI.ClaimReward
{
    public enum ClaimRewardTitle
    {
        GoldPass,
        Congratulations
    }

    public static class ClaimRewardTitleText
    {
        public static string Get(ClaimRewardTitle title)
        {
            return title switch
            {
                ClaimRewardTitle.GoldPass => "Gold Pass",
                ClaimRewardTitle.Congratulations => "Congratulations!",
                _ => throw new ArgumentOutOfRangeException(nameof(title))
            };
        }
    }
}
