using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelDefinition
    {
        private readonly ReadOnlyCollection<GrillDefinition> _grills;

        public LevelDefinition(
            int id,
            LevelDifficulty difficulty,
            int seed,
            PackageSelectionSettings packageSelectionSettings,
            IReadOnlyList<GrillDefinition> grills)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (!Enum.IsDefined(typeof(LevelDifficulty), difficulty))
            {
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            }

            if (grills == null)
            {
                throw new ArgumentNullException(nameof(grills));
            }

            ValidateGrills(grills);

            Id = id;
            Difficulty = difficulty;
            Seed = seed;
            PackageSelectionSettings = packageSelectionSettings ??
                                       throw new ArgumentNullException(nameof(packageSelectionSettings));

            List<GrillDefinition> copiedGrills = new(grills);
            _grills = copiedGrills.AsReadOnly();

            CountAndValidateFoodTokens(out int totalFoodCount, out int uniqueFoodCount);
            TotalFoodCount = totalFoodCount;
            UniqueFoodCount = uniqueFoodCount;
        }

        public int Id { get; }
        public LevelDifficulty Difficulty { get; }
        public int Seed { get; }
        public PackageSelectionSettings PackageSelectionSettings { get; }
        public IReadOnlyList<GrillDefinition> Grills => _grills;
        public int TotalFoodCount { get; }
        public int UniqueFoodCount { get; }

        private void CountAndValidateFoodTokens(out int totalFoodCount, out int uniqueFoodCount)
        {
            totalFoodCount = 0;
            Dictionary<int, int> foodTokenCounts = new();

            for (int i = 0; i < _grills.Count; i++)
            {
                GrillDefinition grill = _grills[i];
                CountFoodTokens(grill.FoodTokenIds, foodTokenCounts, ref totalFoodCount);

                for (int trayIndex = 0; trayIndex < grill.Trays.Count; trayIndex++)
                {
                    TrayDefinition tray = grill.Trays[trayIndex];
                    CountFoodTokens(tray.FoodTokenIds, foodTokenCounts, ref totalFoodCount);
                }
            }

            foreach (KeyValuePair<int, int> foodTokenCount in foodTokenCounts)
            {
                if (foodTokenCount.Value < LevelRules.MinFoodCopies ||
                    foodTokenCount.Value > LevelRules.MaxFoodCopies)
                {
                    throw new ArgumentException(
                        $"Food token {foodTokenCount.Key} must appear between " +
                        $"{LevelRules.MinFoodCopies} and {LevelRules.MaxFoodCopies} times.",
                        nameof(Grills));
                }
            }

            uniqueFoodCount = foodTokenCounts.Count;

            if (uniqueFoodCount < LevelRules.ActivePackageCount)
            {
                throw new ArgumentException(
                    $"Level must contain at least {LevelRules.ActivePackageCount} unique food tokens.",
                    nameof(Grills));
            }
        }

        private static void CountFoodTokens(
            IReadOnlyList<int> foodTokenIds,
            IDictionary<int, int> foodTokenCounts,
            ref int totalFoodCount)
        {
            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (foodTokenId <= 0)
                {
                    continue;
                }

                totalFoodCount++;
                foodTokenCounts.TryGetValue(foodTokenId, out int currentCount);
                foodTokenCounts[foodTokenId] = currentCount + 1;
            }
        }

        private static void ValidateGrills(IReadOnlyList<GrillDefinition> grills)
        {
            if (grills.Count == 0)
            {
                throw new ArgumentException("Level must contain at least one grill.", nameof(grills));
            }

            HashSet<GrillPosition> positions = new();

            for (int i = 0; i < grills.Count; i++)
            {
                GrillDefinition grill = grills[i];

                if (grill == null)
                {
                    throw new ArgumentException("Grill collection cannot contain null.", nameof(grills));
                }

                if (!positions.Add(grill.Position))
                {
                    throw new ArgumentException("Grill positions must be unique.", nameof(grills));
                }
            }
        }
    }
}
