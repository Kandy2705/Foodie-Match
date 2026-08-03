using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardNumberedPlayerRowView :
        LeaderBoardPlayerRowView
    {
        [SerializeField] private TMP_Text _rankText;

        public override void Bind(
            LeaderBoardPlayerData player,
            int rank,
            string valueLabel,
            int value,
            Sprite avatar)
        {
            base.Bind(
                player,
                rank,
                valueLabel,
                value,
                avatar);
            _rankText.text = rank.ToString();
        }
    }
}
