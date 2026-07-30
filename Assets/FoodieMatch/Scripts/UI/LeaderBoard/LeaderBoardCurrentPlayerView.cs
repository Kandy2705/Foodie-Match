using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardCurrentPlayerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _valueLabelText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Image _avatarImage;

        public void Bind(
            LeaderBoardPlayerData player,
            int rank,
            string valueLabel,
            int value,
            Sprite avatar)
        {
            _playerNameText.text = player.displayName;
            _rankText.text =
                rank > 99
                    ? "99+"
                    : rank.ToString();
            _valueLabelText.text = valueLabel;
            _valueText.text = value.ToString();
            _avatarImage.sprite = avatar;
        }
    }
}
