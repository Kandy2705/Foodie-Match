using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;

        public void Bind(
            Sprite icon,
            string amountText)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
            }

            if (_amountText != null)
            {
                _amountText.text = amountText;
            }

            gameObject.SetActive(true);
        }
    }
}
