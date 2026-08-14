using FoodieMatch.Core.Application.Configuration.GoldPass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassRewardItemView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;

        public void Bind(
            GoldPassRewardDefinition reward,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            _iconImage.sprite = visualCatalog.GetIcon(reward);
            _iconImage.preserveAspect = true;

            string amountText =
                GoldPassRewardPresentation.GetAmountText(reward);
            _amountText.gameObject.SetActive(
                !string.IsNullOrEmpty(amountText));
            _amountText.text = amountText;
            gameObject.SetActive(true);
        }
    }
}
