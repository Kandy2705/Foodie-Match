using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfilePopupData : IPopupData
    {
        public ProfilePopupData(
            int currentLevel,
            string playerName = null,
            string joinDate = null,
            Sprite avatarSprite = null,
            Sprite frameSprite = null,
            int firstTryWins = 0,
            int hotPotWins = 0,
            int towerTrial = 0)
        {
            CurrentLevel = currentLevel;
            PlayerName = playerName;
            JoinDate = joinDate;
            AvatarSprite = avatarSprite;
            FrameSprite = frameSprite;
            FirstTryWins = firstTryWins;
            HotPotWins = hotPotWins;
            TowerTrial = towerTrial;
        }

        public int CurrentLevel { get; }
        public string PlayerName { get; }
        public string JoinDate { get; }
        public Sprite AvatarSprite { get; }
        public Sprite FrameSprite { get; }
        public int FirstTryWins { get; }
        public int HotPotWins { get; }
        public int TowerTrial { get; }
    }
}
