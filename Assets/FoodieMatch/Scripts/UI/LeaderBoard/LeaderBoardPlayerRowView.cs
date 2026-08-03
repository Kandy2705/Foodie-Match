using System;
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
        [SerializeField] private Button _giftButton;
        [SerializeField] private Image _giftImage;

        private Action<LeaderBoardPlayerRowView, RectTransform, int>
            _giftClicked;
        private int _rank;

        private void OnEnable()
        {
            if (_giftButton != null)
            {
                _giftButton.onClick.AddListener(OnGiftButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_giftButton != null)
            {
                _giftButton.onClick.RemoveListener(OnGiftButtonClicked);
            }

        }

        private void OnDestroy()
        {
            if (_giftButton != null)
            {
                _giftButton.onClick.RemoveListener(OnGiftButtonClicked);
            }

            _giftClicked = null;
        }

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
            _rank = rank;
        }

        public void SetGiftClickHandler(
            Action<LeaderBoardPlayerRowView, RectTransform, int>
                giftClicked)
        {
            _giftClicked = giftClicked;
        }

        public void HideGift()
        {
            if (_giftImage == null)
            {
                return;
            }

            _giftImage.gameObject.SetActive(false);
        }

        protected void ShowGift(Sprite giftSprite)
        {
            _giftImage.sprite = giftSprite;
            _giftImage.SetNativeSize();
            _giftImage.rectTransform.localScale = Vector3.one;
            _giftImage.gameObject.SetActive(true);
        }

        private void OnGiftButtonClicked()
        {
            if (_giftImage == null ||
                !_giftImage.gameObject.activeInHierarchy)
            {
                return;
            }

            _giftClicked?.Invoke(
                this,
                _giftImage.rectTransform,
                _rank);
        }
    }
}
