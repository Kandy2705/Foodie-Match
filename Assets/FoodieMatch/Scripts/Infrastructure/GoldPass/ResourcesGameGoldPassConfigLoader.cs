using System;
using System.Collections.Generic;
using System.Globalization;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Domain.Booster;
using Newtonsoft.Json;
using UnityEngine;

namespace FoodieMatch.Infrastructure.GoldPass
{
    public sealed class ResourcesGameGoldPassConfigLoader
    {
        private const string ResourcePath = "GoldPass/gold_pass";
        private const long SecondsPerMinute = 60;

        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        public bool TryLoad(
            out IGameGoldPassConfig config,
            out string errorMessage)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                config = null;
                errorMessage =
                    $"Gold Pass config resource {ResourcePath} was not found.";
                return false;
            }

            try
            {
                GoldPassConfigDto configDto =
                    JsonConvert.DeserializeObject<GoldPassConfigDto>(
                        asset.text,
                        _serializerSettings);

                config = MapConfig(configDto);
                errorMessage = null;
                return true;
            }
            catch (Exception exception) when (
                exception is JsonException ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                config = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        private static IGameGoldPassConfig MapConfig(
            GoldPassConfigDto configDto)
        {
            if (configDto?.milestones == null ||
                !Enum.TryParse(
                    configDto.resetDayUtc,
                    ignoreCase: false,
                    out DayOfWeek resetDayUtc) ||
                !Enum.IsDefined(typeof(DayOfWeek), resetDayUtc))
            {
                throw new ArgumentException("Gold Pass config is invalid.");
            }

            List<GoldPassMilestoneDefinition> milestones = new(
                configDto.milestones.Length);

            for (int i = 0; i < configDto.milestones.Length; i++)
            {
                GoldPassMilestoneDto milestoneDto =
                    configDto.milestones[i] ??
                    throw new ArgumentException(
                        "Gold Pass milestone is missing.");

                milestones.Add(
                    new GoldPassMilestoneDefinition(
                        milestoneDto.level,
                        milestoneDto.requiredSpoons,
                        MapReward(milestoneDto.freeReward),
                        MapReward(milestoneDto.seasonReward)));
            }

            return new GameGoldPassConfigSnapshot(
                MapPurchase(configDto.purchase),
                resetDayUtc,
                configDto.resetHourUtc,
                milestones);
        }

        private static GoldPassPurchaseDefinition MapPurchase(
            GoldPassPurchaseDto purchaseDto)
        {
            if (purchaseDto == null)
            {
                throw new ArgumentException(
                    "Gold Pass purchase config is missing.");
            }

            return new GoldPassPurchaseDefinition(
                purchaseDto.storeProductId,
                purchaseDto.fallbackDisplayPrice);
        }

        private static GoldPassRewardDefinition MapReward(
            GoldPassRewardDto rewardDto)
        {
            if (rewardDto == null ||
                !Enum.TryParse(
                    rewardDto.type,
                    ignoreCase: false,
                    out GoldPassRewardType rewardType) ||
                !Enum.IsDefined(typeof(GoldPassRewardType), rewardType))
            {
                throw new ArgumentException("Gold Pass reward is invalid.");
            }

            switch (rewardType)
            {
                case GoldPassRewardType.Coin:
                    return GoldPassRewardDefinition.CreateCoin(rewardDto.amount);

                case GoldPassRewardType.UnlimitedHeart:
                    return GoldPassRewardDefinition.CreateUnlimitedHeart(
                        checked(rewardDto.durationMinutes * SecondsPerMinute));

                case GoldPassRewardType.Booster:
                    if (!Enum.TryParse(
                            rewardDto.boosterType,
                            ignoreCase: false,
                            out BoosterType boosterType) ||
                        !Enum.IsDefined(typeof(BoosterType), boosterType))
                    {
                        throw new ArgumentException(
                            "Gold Pass booster reward is invalid.");
                    }

                    return GoldPassRewardDefinition.CreateBooster(
                        boosterType,
                        checked((int)rewardDto.amount));

                case GoldPassRewardType.Treasure1:
                case GoldPassRewardType.Treasure2:
                case GoldPassRewardType.Treasure3:
                    return GoldPassRewardDefinition.CreateTreasure(
                        rewardType,
                        MapTreasureContents(rewardDto.contents));

                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardType));
            }
        }

        private static IReadOnlyList<GoldPassRewardDefinition> MapTreasureContents(
            GoldPassRewardDto[] contents)
        {
            if (contents == null)
            {
                return Array.Empty<GoldPassRewardDefinition>();
            }

            List<GoldPassRewardDefinition> rewards = new(contents.Length);

            for (int i = 0; i < contents.Length; i++)
            {
                rewards.Add(MapReward(contents[i]));
            }

            return rewards;
        }
    }
}
