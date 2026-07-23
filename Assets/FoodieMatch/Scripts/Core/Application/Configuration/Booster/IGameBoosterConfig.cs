using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Booster
{
    public interface IGameBoosterConfig
    {
        int GetUnlockLevel(BoosterType boosterType);
    }
}
