using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Domain.GoldPass;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassService
    {
        private readonly IGameGoldPassConfig _config;
        private readonly PlayerProfileService _playerProfileService;
        private readonly IClock _clock;
        private readonly GoldPassSeasonSchedule _seasonSchedule;

        public GoldPassService(
            IGameGoldPassConfig config,
            PlayerProfileService playerProfileService,
            IClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _seasonSchedule = new GoldPassSeasonSchedule(config);
        }

        public GoldPassStatus GetStatus()
        {
            GoldPassSeasonPeriod season = GetCurrentSeason();
            GoldPassState state =
                _playerProfileService.RefreshGoldPassSeason(season.SeasonId);
            IReadOnlyList<GoldPassMilestoneDefinition> definitions =
                _config.Milestones;
            List<GoldPassMilestoneStatus> milestones = new(definitions.Count);
            int? nextMilestoneLevel = null;
            int previousRequiredSpoons = 0;
            int currentSegmentSpoons = 0;
            int requiredSegmentSpoons = 0;

            for (int i = 0; i < definitions.Count; i++)
            {
                GoldPassMilestoneDefinition definition = definitions[i];
                bool isUnlocked = state.SpoonCount >= definition.RequiredSpoons;

                milestones.Add(
                    new GoldPassMilestoneStatus(
                        definition,
                        isUnlocked,
                        state.HasClaimedFreeMilestone(definition.Level),
                        state.HasClaimedSeasonMilestone(definition.Level)));

                if (!nextMilestoneLevel.HasValue && !isUnlocked)
                {
                    nextMilestoneLevel = definition.Level;
                    currentSegmentSpoons =
                        state.SpoonCount - previousRequiredSpoons;
                    requiredSegmentSpoons =
                        definition.RequiredSpoons - previousRequiredSpoons;
                }

                previousRequiredSpoons = definition.RequiredSpoons;
            }

            return new GoldPassStatus(
                season,
                state.SpoonCount,
                state.IsSeasonPassPurchased,
                nextMilestoneLevel,
                currentSegmentSpoons,
                requiredSegmentSpoons,
                milestones);
        }

        public void AddSpoons(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _playerProfileService.AddGoldPassSpoons(
                GetCurrentSeason().SeasonId,
                amount);
        }

        public Task<bool> ActivateSeasonPassAsync()
        {
            return _playerProfileService.TryActivateGoldPassSeasonPassAsync(
                GetCurrentSeason().SeasonId);
        }

        public void ApplyDebugUpdate(
            int spoonCount,
            bool isSeasonPassPurchased)
        {
            _playerProfileService.ApplyGoldPassDebugUpdate(
                GetCurrentSeason().SeasonId,
                spoonCount,
                isSeasonPassPurchased);
        }

        public void ResetClaimHistory()
        {
            _playerProfileService.ResetGoldPassClaimHistory(
                GetCurrentSeason().SeasonId);
        }

        public GoldPassClaimResult TryClaim(
            int milestoneLevel,
            GoldPassTrack track)
        {
            if (!_config.TryGetMilestone(
                    milestoneLevel,
                    out GoldPassMilestoneDefinition milestone))
            {
                return GoldPassClaimResult.MilestoneNotFound;
            }

            return _playerProfileService.TryClaimGoldPassReward(
                GetCurrentSeason().SeasonId,
                milestone,
                track);
        }

        private GoldPassSeasonPeriod GetCurrentSeason()
        {
            return _seasonSchedule.GetCurrentSeason(_clock.UtcNow);
        }
    }
}
