using System;
using System.Collections.Generic;
using FoodieMatch.UI.Popup;

namespace FoodieMatch.UI.ClaimReward
{
    public sealed class ClaimRewardPopupData : IPopupData
    {
        public ClaimRewardPopupData(
            ClaimRewardTitle title,
            IReadOnlyList<ClaimRewardItemData> rewards,
            Action continued = null,
            float presentationScale = 1f)
        {
            if (presentationScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationScale));
            }

            Title = title;
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            Continued = continued;
            PresentationScale = presentationScale;
        }

        public ClaimRewardTitle Title { get; }

        public IReadOnlyList<ClaimRewardItemData> Rewards { get; }

        public Action Continued { get; }

        public float PresentationScale { get; }
    }
}
