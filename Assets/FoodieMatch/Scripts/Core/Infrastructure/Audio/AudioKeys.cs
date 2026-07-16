using UnityEngine;

namespace FoodieMatch.Core.Infrastructure.Audio
{
    public static class AudioKeys
    {
        public const string MusicMenu = "menu";
        public const string MusicIngame = "ingame";

        public const string SfxClick = "click";
        public const string SfxScreenTap = "screen-tap";
        public const string SfxWinGame = "win_screen";
        public const string SfxLoseGame = "Lose-game";
        public const string SfxClaim = "sfx_claim";
        public const string SfxFail = "sfx_fail";
        public const string SfxCoinReceive = "coin_receive";
        public const string SfxSelectSkewer = "select_skewer";
        public const string SfxBoosterCollect = "Booster_Collect";
        public const string SfxBoosterShuffle = "booster_shuffle";

        public static readonly string[] SfxMergeCombo =
        {
            "Items_Merge_combo_1",
            "Items_Merge_combo_2",
            "Items_Merge_combo_3",
            "Items_Merge_combo_4",
            "Items_Merge_combo_5",
            "Items_Merge_combo_6",
            "Items_Merge_combo_7",
            "Items_Merge_combo_8",
            "Items_Merge_combo_9",
            "Items_Merge_combo_10"
        };

        public static string GetMergeComboSfx(int comboCount)
        {
            if (SfxMergeCombo.Length == 0)
            {
                return string.Empty;
            }

            int index = Mathf.Clamp(
                comboCount - 1,
                0,
                SfxMergeCombo.Length - 1);

            return SfxMergeCombo[index];
        }
    }
}

