using System;
using System.Collections.Generic;
using System.Linq;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.Data.Booster;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    internal sealed class PlayerProfileMapper
    {
        public bool TryMapToRecord(
            PlayerProfileDto profileDto,
            out PlayerProfileRecord record,
            out string errorMessage)
        {
            if (profileDto == null)
            {
                throw new ArgumentNullException(nameof(profileDto));
            }

            if (profileDto.Revision < 0)
            {
                record = null;
                errorMessage = "Player profile revision cannot be negative.";
                return false;
            }

            if (!TryMapBoosterCounts(
                    profileDto.BoosterCounts,
                    out Dictionary<BoosterType, int> boosterCounts,
                    out errorMessage) ||
                !TryMapSeenBoosterGuides(
                    profileDto.SeenBoosterGuides,
                    out List<BoosterType> seenBoosterGuides,
                    out errorMessage))
            {
                record = null;
                return false;
            }

            try
            {
                PlayerProfile profile = new(
                    profileDto.CurrentLevelNumber,
                    boosterCounts,
                    seenBoosterGuides);
                record = new PlayerProfileRecord(profile, profileDto.Revision);
                errorMessage = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                record = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        public PlayerProfileDto MapToDto(PlayerProfile profile, long revision)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision),
                    revision,
                    "Revision cannot be negative.");
            }

            return new PlayerProfileDto
            {
                SchemaVersion = PlayerProfileDataVersions.Current,
                Revision = revision,
                CurrentLevelNumber = profile.CurrentLevelNumber,
                BoosterCounts = profile.BoosterCounts
                    .OrderBy(boosterCount => (int)boosterCount.Key)
                    .Select(
                        boosterCount => new BoosterCountDto
                        {
                            BoosterType = (int)boosterCount.Key,
                            Count = boosterCount.Value
                        })
                    .ToList(),
                SeenBoosterGuides = profile.SeenBoosterGuides
                    .Select(boosterType => (int)boosterType)
                    .OrderBy(boosterType => boosterType)
                    .ToList()
            };
        }

        private static bool TryMapBoosterCounts(
            IReadOnlyCollection<BoosterCountDto> boosterCountDtos,
            out Dictionary<BoosterType, int> boosterCounts,
            out string errorMessage)
        {
            boosterCounts = new Dictionary<BoosterType, int>();

            if (boosterCountDtos == null)
            {
                errorMessage = "Player profile booster counts are missing.";
                return false;
            }

            foreach (BoosterCountDto boosterCountDto in boosterCountDtos)
            {
                if (boosterCountDto == null)
                {
                    errorMessage = "Player profile contains an empty booster count entry.";
                    return false;
                }

                BoosterType boosterType = (BoosterType)boosterCountDto.BoosterType;

                if (!Enum.IsDefined(typeof(BoosterType), boosterType))
                {
                    errorMessage =
                        $"Booster type {boosterCountDto.BoosterType} is not defined.";
                    return false;
                }

                if (boosterCountDto.Count < 0)
                {
                    errorMessage = $"Booster {boosterType} count cannot be negative.";
                    return false;
                }

                if (!boosterCounts.TryAdd(boosterType, boosterCountDto.Count))
                {
                    errorMessage = $"Booster {boosterType} count is duplicated.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool TryMapSeenBoosterGuides(
            IReadOnlyCollection<int> boosterTypeValues,
            out List<BoosterType> seenBoosterGuides,
            out string errorMessage)
        {
            seenBoosterGuides = new List<BoosterType>();

            if (boosterTypeValues == null)
            {
                errorMessage = "Player profile seen booster guides are missing.";
                return false;
            }

            HashSet<BoosterType> uniqueBoosterTypes = new();

            foreach (int boosterTypeValue in boosterTypeValues)
            {
                BoosterType boosterType = (BoosterType)boosterTypeValue;

                if (!Enum.IsDefined(typeof(BoosterType), boosterType))
                {
                    errorMessage = $"Booster guide type {boosterTypeValue} is not defined.";
                    return false;
                }

                if (!uniqueBoosterTypes.Add(boosterType))
                {
                    errorMessage = $"Booster guide {boosterType} is duplicated.";
                    return false;
                }

                seenBoosterGuides.Add(boosterType);
            }

            errorMessage = null;
            return true;
        }
    }
}
