using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelDefinition
    {
        private const float PositionTolerance = 0.001f;

        private readonly ReadOnlyCollection<GrillDefinition> _grills;
        private readonly ReadOnlyCollection<GrillMovementGroupDefinition>
            _movingGrillGroups;

        public LevelDefinition(
            int id,
            LevelDifficulty difficulty,
            LevelRandomSettings randomSettings,
            PackageSelectionSettings packageSelectionSettings,
            IReadOnlyList<GrillMovementGroupDefinition> movingGrillGroups,
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

            if (movingGrillGroups == null)
            {
                throw new ArgumentNullException(nameof(movingGrillGroups));
            }

            ValidateGrills(grills);
            ValidateMovingGrillGroups(grills, movingGrillGroups);

            Id = id;
            Difficulty = difficulty;
            RandomSettings = randomSettings ?? throw new ArgumentNullException(nameof(randomSettings));
            PackageSelectionSettings = packageSelectionSettings ??
                                       throw new ArgumentNullException(nameof(packageSelectionSettings));

            List<GrillDefinition> copiedGrills = new(grills);
            List<GrillMovementGroupDefinition> copiedMovementGroups =
                new(movingGrillGroups);
            _grills = copiedGrills.AsReadOnly();
            _movingGrillGroups = copiedMovementGroups.AsReadOnly();

            CountAndValidateFoodTokens(out int totalFoodCount, out int uniqueFoodCount);
            TotalFoodCount = totalFoodCount;
            UniqueFoodCount = uniqueFoodCount;
        }

        public int Id { get; }
        public LevelDifficulty Difficulty { get; }
        public LevelRandomSettings RandomSettings { get; }
        public PackageSelectionSettings PackageSelectionSettings { get; }
        public IReadOnlyList<GrillMovementGroupDefinition> MovingGrillGroups =>
            _movingGrillGroups;
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

            HashSet<int> grillIds = new();
            HashSet<GrillPosition> positions = new();

            for (int i = 0; i < grills.Count; i++)
            {
                GrillDefinition grill = grills[i];

                if (grill == null)
                {
                    throw new ArgumentException("Grill collection cannot contain null.", nameof(grills));
                }

                if (!grillIds.Add(grill.Id))
                {
                    throw new ArgumentException("Grill ids must be unique.", nameof(grills));
                }

                if (!positions.Add(grill.Position))
                {
                    throw new ArgumentException("Grill positions must be unique.", nameof(grills));
                }
            }
        }

        private static void ValidateMovingGrillGroups(
            IReadOnlyList<GrillDefinition> grills,
            IReadOnlyList<GrillMovementGroupDefinition> movementGroups)
        {
            Dictionary<int, GrillDefinition> grillsById = new();

            for (int i = 0; i < grills.Count; i++)
            {
                grillsById.Add(grills[i].Id, grills[i]);
            }

            HashSet<int> movingGrillIds = new();

            for (int i = 0; i < movementGroups.Count; i++)
            {
                GrillMovementGroupDefinition movementGroup = movementGroups[i];

                if (movementGroup == null)
                {
                    throw new ArgumentException(
                        "Movement group collection cannot contain null.",
                        nameof(movementGroups));
                }

                List<float> movementPositions = new(movementGroup.GrillIds.Count);
                bool isHorizontal = IsHorizontal(movementGroup.Direction);
                float fixedPosition = 0f;

                for (int grillIndex = 0;
                     grillIndex < movementGroup.GrillIds.Count;
                     grillIndex++)
                {
                    int grillId = movementGroup.GrillIds[grillIndex];

                    if (!grillsById.TryGetValue(grillId, out GrillDefinition grill))
                    {
                        throw new ArgumentException(
                            $"Movement group references missing grill id {grillId}.",
                            nameof(movementGroups));
                    }

                    if (!movingGrillIds.Add(grillId))
                    {
                        throw new ArgumentException(
                            $"Grill id {grillId} belongs to multiple movement groups.",
                            nameof(movementGroups));
                    }

                    float currentFixedPosition =
                        isHorizontal ? grill.Position.Y : grill.Position.X;
                    float movementPosition =
                        isHorizontal ? grill.Position.X : grill.Position.Y;

                    if (grillIndex == 0)
                    {
                        fixedPosition = currentFixedPosition;
                    }
                    else if (!Approximately(fixedPosition, currentFixedPosition))
                    {
                        throw new ArgumentException(
                            $"Movement group {i} grills must be on the same line.",
                            nameof(movementGroups));
                    }

                    movementPositions.Add(movementPosition);
                }

                ValidateEqualSpacing(movementPositions, i, movementGroups);
            }
        }

        private static void ValidateEqualSpacing(
            List<float> positions,
            int movementGroupIndex,
            IReadOnlyList<GrillMovementGroupDefinition> movementGroups)
        {
            positions.Sort();
            float expectedSpacing = positions[1] - positions[0];

            if (expectedSpacing <= PositionTolerance)
            {
                throw new ArgumentException(
                    $"Movement group {movementGroupIndex} grill spacing must be greater than zero.",
                    nameof(movementGroups));
            }

            for (int i = 2; i < positions.Count; i++)
            {
                float spacing = positions[i] - positions[i - 1];

                if (!Approximately(expectedSpacing, spacing))
                {
                    throw new ArgumentException(
                        $"Movement group {movementGroupIndex} grills must be evenly spaced.",
                        nameof(movementGroups));
                }
            }
        }

        private static bool IsHorizontal(GrillMovementDirection direction)
        {
            return direction == GrillMovementDirection.Left ||
                   direction == GrillMovementDirection.Right;
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= PositionTolerance;
        }
    }
}
