using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public class LeaderBoardPlayerRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _valueLabelText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Image _avatarImage;

        public virtual void Bind(
            LeaderBoardPlayerData player,
            int rank,
            string valueLabel,
            int value,
            Sprite avatar)
        {
            _playerNameText.text = player.displayName;
            _valueLabelText.text = valueLabel;
            _valueText.text = value.ToString();
            _avatarImage.sprite = avatar;
        }
    }
}
