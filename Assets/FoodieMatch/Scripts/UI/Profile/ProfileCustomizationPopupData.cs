using FoodieMatch.UI.Popup;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfileCustomizationPopupData : IPopupData
    {
        public ProfileCustomizationPopupData(
            string playerName,
            string avatarId,
            string frameId,
            ProfileCustomizationCatalogSO catalog)
        {
            PlayerName = playerName;
            AvatarId = avatarId;
            FrameId = frameId;
            Catalog = catalog;
        }

        public string PlayerName { get; }

        public string AvatarId { get; }

        public string FrameId { get; }

        public ProfileCustomizationCatalogSO Catalog { get; }
    }
}
