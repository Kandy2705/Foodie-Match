using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.GoldPass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassMilestoneView : MonoBehaviour
    {
        [Header("Milestone")]
        [SerializeField] private Image _levelPanel;
        [SerializeField] private Sprite _completedLevelSprite;
        [SerializeField] private Sprite _incompleteLevelSprite;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private GameObject _verticalColumn;
        [SerializeField] private GameObject _breakLine;
        [SerializeField] private GameObject _boldBreakLine;

        [Header("Rewards")]
        [SerializeField] private GoldPassRewardTrackView _freeRewardView;
        [SerializeField] private GoldPassRewardTrackView _seasonRewardView;

        private Action<int, GoldPassTrack, GoldPassRewardDefinition>
            _claimClicked;
        private Action<int, GoldPassTrack, GoldPassRewardDefinition,
                RectTransform>
            _treasureClicked;
        private GoldPassMilestoneDefinition _definition;

        public void Bind(
            GoldPassMilestoneStatus milestone,
            bool isSeasonPassPurchased,
            bool isCurrentMilestone,
            GoldPassRewardVisualCatalogSO visualCatalog,
            Action<int, GoldPassTrack, GoldPassRewardDefinition> claimClicked,
            Action<int, GoldPassTrack, GoldPassRewardDefinition,
                    RectTransform>
                treasureClicked)
        {
            _definition = milestone.Definition;
            _claimClicked = claimClicked;
            _treasureClicked = treasureClicked;

            _levelPanel.sprite = milestone.IsUnlocked
                ? _completedLevelSprite
                : _incompleteLevelSprite;
            _levelText.text = _definition.Level.ToString();
            _verticalColumn.SetActive(milestone.IsUnlocked);
            _breakLine.SetActive(!isCurrentMilestone);
            _boldBreakLine.SetActive(isCurrentMilestone);

            _freeRewardView.Bind(
                _definition.FreeReward,
                visualCatalog,
                milestone.IsUnlocked,
                true,
                milestone.IsFreeRewardClaimed,
                OnFreeRewardClaimed,
                OnFreeTreasureClicked);

            _seasonRewardView.Bind(
                _definition.SeasonReward,
                visualCatalog,
                milestone.IsUnlocked,
                isSeasonPassPurchased,
                milestone.IsSeasonRewardClaimed,
                OnSeasonRewardClaimed,
                OnSeasonTreasureClicked);

            gameObject.SetActive(true);
        }

        public void Clear()
        {
            _freeRewardView.Clear();
            _seasonRewardView.Clear();
            _claimClicked = null;
            _treasureClicked = null;
            _definition = null;
        }

        private void OnFreeRewardClaimed()
        {
            _claimClicked(
                _definition.Level,
                GoldPassTrack.Free,
                _definition.FreeReward);
        }

        private void OnSeasonRewardClaimed()
        {
            _claimClicked(
                _definition.Level,
                GoldPassTrack.Season,
                _definition.SeasonReward);
        }

        private void OnFreeTreasureClicked(RectTransform source)
        {
            _treasureClicked(
                _definition.Level,
                GoldPassTrack.Free,
                _definition.FreeReward,
                source);
        }

        private void OnSeasonTreasureClicked(RectTransform source)
        {
            _treasureClicked(
                _definition.Level,
                GoldPassTrack.Season,
                _definition.SeasonReward,
                source);
        }
    }
}
