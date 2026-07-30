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
    }
}
