using System;
using System.Collections.Generic;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.Booster;
using FoodieMatch.UI.BoosterBuy;
using FoodieMatch.UI.BoosterGuide;
using FoodieMatch.UI.Debugging;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Heart;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.LeaderBoard;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Pause;
using FoodieMatch.UI.Result;
using FoodieMatch.UI.RetryGame;
using FoodieMatch.UI.Revive;
using FoodieMatch.UI.Setting;
using FoodieMatch.UI.Shop;
using FoodieMatch.UI.StarterPack;

namespace FoodieMatch.UI.AddressableAssets
{
    public static class UiAddressCatalog
    {
        private static readonly IReadOnlyDictionary<Type, string> Addresses =
            new Dictionary<Type, string>
            {
                [typeof(LoseView)] = UiAddressKeys.LevelLosePopup,
                [typeof(WinView)] = UiAddressKeys.LevelCompletePopup,
                [typeof(LeaveGamePopupView)] = UiAddressKeys.LeaveGamePopup,
                [typeof(GameplayHudView)] = UiAddressKeys.GameplayRoot,
                [typeof(FillHeartPopupView)] = UiAddressKeys.FillYourHeartPopup,
                [typeof(FakeRewardedAdPopupView)] = UiAddressKeys.FakeRewardedAdPopup,
                [typeof(PlayerDebugPopupView)] = UiAddressKeys.DebugPopup,
                [typeof(BoosterSwapPopup)] = UiAddressKeys.BoosterSwapPopup,
                [typeof(BoosterGuidePopupView)] = UiAddressKeys.BoosterGuidePopup,
                [typeof(BoosterBuyPopupView)] = UiAddressKeys.BoosterBuyPopup,
                [typeof(MainMenuView)] = UiAddressKeys.MainMenuRoot,
                [typeof(PauseView)] = UiAddressKeys.PausePopup,
                [typeof(RetryGamePopupView)] = UiAddressKeys.RetryGamePopup,
                [typeof(RevivePopupView)] = UiAddressKeys.RevivePopup,
                [typeof(SettingPopupView)] = UiAddressKeys.SettingsPopup,
                [typeof(StarterPackPopupView)] =
                    UiAddressKeys.StarterPackPopup,
                [typeof(ShopView)] = UiAddressKeys.ShopScreen,
                [typeof(LeaderBoardView)] = UiAddressKeys.LeaderBoardScreen
            };

        public static bool TryGetAddress(Type uiType, out string address)
        {
            if (uiType == null)
            {
                throw new ArgumentNullException(nameof(uiType));
            }

            return Addresses.TryGetValue(uiType, out address);
        }

        public static string GetAddress<T>()
        {
            Type uiType = typeof(T);

            if (TryGetAddress(uiType, out string address))
            {
                return address;
            }

            throw new KeyNotFoundException(
                $"No Addressables UI address is registered for {uiType.FullName}.");
        }
    }
}
