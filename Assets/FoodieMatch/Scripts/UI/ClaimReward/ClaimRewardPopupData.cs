using System;
using System.Collections.Generic;
using FoodieMatch.UI.Popup;

namespace FoodieMatch.UI.ClaimReward
{
    public sealed class ClaimRewardPopupData : IPopupData
    {
        public ClaimRewardPopupData(
            ClaimRewardTitle title,
            IReadOnlyList<ClaimRewardItemData> rewards)
        {
            Title = title;
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }

        public ClaimRewardTitle Title { get; }

        public IReadOnlyList<ClaimRewardItemData> Rewards { get; }
    }
}
