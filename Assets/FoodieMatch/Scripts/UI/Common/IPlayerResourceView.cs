using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.UI.Common
{
    public interface IPlayerResourceView
    {
        void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus);
    }
}
