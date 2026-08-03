using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardMedalPlayerRowView :
        LeaderBoardPlayerRowView
    {
        [SerializeField] private Image _medalImage;
        [SerializeField] private Sprite _goldMedalSprite;
        [SerializeField] private Sprite _silverMedalSprite;
        [SerializeField] private Sprite _bronzeMedalSprite;
        [SerializeField] private Image _giftImage;
        [SerializeField] private Sprite _firstPlaceGiftSprite;
        [SerializeField] private Sprite _secondPlaceGiftSprite;
        [SerializeField] private Sprite _thirdPlaceGiftSprite;

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

            _medalImage.sprite = rank switch
            {
                1 => _goldMedalSprite,
                2 => _silverMedalSprite,
                3 => _bronzeMedalSprite,
                _ => throw new InvalidOperationException(
                    $"A medal row cannot display rank {rank}.")
            };
        }

        public void ShowWeeklyGift(
            int rank)
        {
            _giftImage.sprite = rank switch
            {
                1 => _firstPlaceGiftSprite,
                2 => _secondPlaceGiftSprite,
                3 => _thirdPlaceGiftSprite,
                _ => throw new InvalidOperationException(
                    $"A weekly gift cannot display rank {rank}.")
            };
            _giftImage.SetNativeSize();
            _giftImage.gameObject.SetActive(true);
        }

    }
}
