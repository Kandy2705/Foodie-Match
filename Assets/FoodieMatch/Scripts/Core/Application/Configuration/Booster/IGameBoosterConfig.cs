using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Booster
{
    public interface IGameBoosterConfig
    {
        int UnlockRewardAmount { get; }

        int GetUnlockLevel(BoosterType boosterType);
    }
}
