using System;

namespace FoodieMatch.UI.Result
{
    public sealed class WinViewActions
    {
        public WinViewActions(Action claimCoinRewardClicked, Action doubleCoinRewardClicked)
        {
            ClaimCoinRewardClicked = claimCoinRewardClicked
                ?? throw new ArgumentNullException(nameof(claimCoinRewardClicked));
            DoubleCoinRewardClicked = doubleCoinRewardClicked
                ?? throw new ArgumentNullException(nameof(doubleCoinRewardClicked));
        }

        public Action ClaimCoinRewardClicked { get; }

        public Action DoubleCoinRewardClicked { get; }
    }
}
