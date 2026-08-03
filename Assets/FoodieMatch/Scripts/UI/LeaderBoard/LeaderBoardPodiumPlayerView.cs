using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardPodiumPlayerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Image _avatarImage;

        public void Bind(
            LeaderBoardPlayerData player,
            Sprite avatar)
        {
            _playerNameText.text = player.displayName;
            _avatarImage.sprite = avatar;
        }
    }
}
