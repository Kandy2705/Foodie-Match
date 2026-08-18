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
        [SerializeField] private Image _avatarFrameImage;

        public void Bind(
            LeaderBoardPlayerData player,
            int rank,
            string valueLabel,
            int value,
            Sprite avatar,
            Sprite frame = null)
        {
            if (_playerNameText != null && player != null)
            {
                _playerNameText.text = player.displayName;
            }

            if (_rankText != null)
            {
                _rankText.text =
                    rank > 99
                        ? "99+"
                        : rank.ToString();
            }

            if (_valueLabelText != null)
            {
                _valueLabelText.text = valueLabel;
            }

            if (_valueText != null)
            {
                _valueText.text = value.ToString();
            }

            if (_avatarImage != null && avatar != null)
            {
                _avatarImage.sprite = avatar;
            }

            if (_avatarFrameImage != null && frame != null)
            {
                _avatarFrameImage.sprite = frame;
            }
        }

        public void SetCustomization(
            string playerName,
            Sprite avatar,
            Sprite frame = null)
        {
            if (_playerNameText != null && !string.IsNullOrEmpty(playerName))
            {
                _playerNameText.text = playerName;
            }

            if (_avatarImage != null && avatar != null)
            {
                _avatarImage.sprite = avatar;
            }

            if (_avatarFrameImage != null && frame != null)
            {
                _avatarFrameImage.sprite = frame;
            }
        }
    }
}
