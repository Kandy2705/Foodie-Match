using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Fridge;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Core.Application.UseCases
{
    public sealed class RequiredPackageGenerator
    {
        public bool TryCreatePackage(
            BoardModel board,
            WaitingRackModel waitingRack,
            FridgeInventoryModel fridge,
            IReadOnlyList<RequiredPackageModel> packageReservations,
            PackageSelectionWeights weights,
            PackageRandom random,
            out RequiredPackageModel package)
        {
            package = null;

            if (board == null ||
                waitingRack == null ||
                packageReservations == null ||
                weights == null ||
                random == null)
            {
                return false;
            }

            HashSet<int> reservedFoodIds = GetReservedFoodIds(packageReservations);
            Dictionary<int, FoodAvailability> availabilityByFoodId =
                CreateFoodAvailability(board, waitingRack, fridge, reservedFoodIds);

            if (!TrySelectFullyRevealedFood(
                    availabilityByFoodId.Values,
                    weights,
                    random,
                    out FoodAvailability selected) &&
                !TrySelectMostCommonGrillFood(
                    board,
                    availabilityByFoodId,
                    random,
                    out selected) &&
                !TrySelectNearestTrayFood(
                    board,
                    availabilityByFoodId,
                    random,
                    out selected))
            {
                return false;
            }

            package = new RequiredPackageModel(selected.FoodId, selected.TotalCount, filledAmount: 0);
            return true;
        }

        private static HashSet<int> GetReservedFoodIds(IReadOnlyList<RequiredPackageModel> packages)
        {
            HashSet<int> foodIds = new();

            for (int i = 0; i < packages.Count; i++)
            {
                RequiredPackageModel package = packages[i];

                if (package != null)
                {
                    foodIds.Add(package.FoodTokenId);
                }
            }

            return foodIds;
        }

        private static Dictionary<int, FoodAvailability> CreateFoodAvailability(
            BoardModel board,
            WaitingRackModel waitingRack,
            FridgeInventoryModel fridge,
            ISet<int> reservedFoodIds)
        {
            Dictionary<int, FoodAvailability> availabilityByFoodId = new();
            AddFoodIds(
                waitingRack.GetFoodTokenIds(),
                FoodLocation.WaitingRack,
                reservedFoodIds,
                availabilityByFoodId);

            if (fridge != null && !fridge.IsEmpty)
            {
                AddFoodIds(
                    fridge.GetAllTokenIds(),
                    FoodLocation.Fridge,
                    reservedFoodIds,
                    availabilityByFoodId);
            }

            if (board.LayoutType == GrillLayoutType.StackedColumns)
            {
                AddStackedGrillFood(board, reservedFoodIds, availabilityByFoodId);
            }
            else
            {
                AddStandardGrillFood(board, reservedFoodIds, availabilityByFoodId);
            }

            return availabilityByFoodId;
        }

        private static void AddStandardGrillFood(
            BoardModel board,
            ISet<int> reservedFoodIds,
            IDictionary<int, FoodAvailability> availabilityByFoodId)
        {
            for (int depth = 0; depth < board.FoodDepthCount; depth++)
            {
                FoodLocation location = depth switch
                {
                    0 => FoodLocation.Grill,
                    1 => FoodLocation.TopTray,
                    _ => FoodLocation.DeepTray
                };

                AddFoodIds(
                    board.GetFoodTokenIdsAtDepth(depth),
                    location,
                    reservedFoodIds,
                    availabilityByFoodId);
            }
        }

        private static void AddStackedGrillFood(
            BoardModel board,
            ISet<int> reservedFoodIds,
            IDictionary<int, FoodAvailability> availabilityByFoodId)
        {
            for (int grillIndex = 0; grillIndex < board.GrillCount; grillIndex++)
            {
                GrillModel grill = board.GetGrillAt(grillIndex);
                List<int> foodIds = new(grill.ActiveFoodSlotCount);

                for (int slotIndex = 0;
                     slotIndex < grill.ActiveFoodSlotCount;
                     slotIndex++)
                {
                    foodIds.Add(grill.GetFoodTokenIdAt(slotIndex));
                }

                AddFoodIds(
                    foodIds,
                    GetStackedGrillLocation(board, grill.PositionIndex),
                    reservedFoodIds,
                    availabilityByFoodId);
            }
        }

        private static FoodLocation GetStackedGrillLocation(
            BoardModel board,
            int grillPositionIndex)
        {
            return board.IsGrillInActiveRows(grillPositionIndex)
                ? FoodLocation.Grill
                : FoodLocation.DeepTray;
        }

        private static void AddFoodIds(
            IReadOnlyList<int> foodIds,
            FoodLocation location,
            ISet<int> reservedFoodIds,
            IDictionary<int, FoodAvailability> availabilityByFoodId)
        {
            for (int i = 0; i < foodIds.Count; i++)
            {
                int foodId = foodIds[i];

                if (foodId <= BoardRules.EmptyFoodTokenId || reservedFoodIds.Contains(foodId))
                {
                    continue;
                }

                if (!availabilityByFoodId.TryGetValue(foodId, out FoodAvailability availability))
                {
                    availability = new FoodAvailability(foodId);
                    availabilityByFoodId.Add(foodId, availability);
                }

                availability.Add(location);
            }
        }

        private static bool TrySelectFullyRevealedFood(
            ICollection<FoodAvailability> availabilities,
            PackageSelectionWeights weights,
            PackageRandom random,
            out FoodAvailability selected)
        {
            List<FoodAvailability> rackRescue = new();
            List<FoodAvailability> readyNow = new();
            List<FoodAvailability> topTray = new();

            foreach (FoodAvailability availability in availabilities)
            {
                if (!availability.IsFullyRevealed)
                {
                    continue;
                }

                if (availability.WaitingRackCount > 0)
                {
                    rackRescue.Add(availability);
                }
                else if (
                    availability.GrillCount > 0 &&
                    availability.TopTrayCount > 0)
                {
                    topTray.Add(availability);
                }
                else if (
                    availability.TopTrayCount == 0 &&
                    (availability.GrillCount > 0 ||
                     availability.FridgeCount > 0))
                {
                    readyNow.Add(availability);
                }
            }

            List<FoodCandidateGroup> groups = new()
            {
                new FoodCandidateGroup(rackRescue, weights.RackRescueWeight),
                new FoodCandidateGroup(readyNow, weights.ReadyNowWeight),
                new FoodCandidateGroup(topTray, weights.TopTrayWeight)
            };

            return TrySelectFromGroups(groups, random, out selected);
        }

        private static bool TrySelectFromGroups(
            IReadOnlyList<FoodCandidateGroup> groups,
            PackageRandom random,
            out FoodAvailability selected)
        {
            selected = null;
            long totalWeight = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                FoodCandidateGroup group = groups[i];

                if (group.Candidates.Count > 0 && group.Weight > 0)
                {
                    totalWeight += group.Weight;
                }
            }

            if (totalWeight > 0)
            {
                long selectedWeight = random.NextWeight(totalWeight);

                for (int i = 0; i < groups.Count; i++)
                {
                    FoodCandidateGroup group = groups[i];

                    if (group.Candidates.Count == 0 || group.Weight <= 0)
                    {
                        continue;
                    }

                    if (selectedWeight < group.Weight)
                    {
                        selected = SelectRandomCandidate(group.Candidates, random);
                        return true;
                    }

                    selectedWeight -= group.Weight;
                }
            }

            List<FoodAvailability> fallbackCandidates = new();

            for (int i = 0; i < groups.Count; i++)
            {
                fallbackCandidates.AddRange(groups[i].Candidates);
            }

            if (fallbackCandidates.Count == 0)
            {
                return false;
            }

            selected = SelectRandomCandidate(fallbackCandidates, random);
            return true;
        }

        private static bool TrySelectMostCommonGrillFood(
            BoardModel board,
            IReadOnlyDictionary<int, FoodAvailability> availabilityByFoodId,
            PackageRandom random,
            out FoodAvailability selected)
        {
            IReadOnlyList<int> foodIds = board.LayoutType == GrillLayoutType.StackedColumns
                ? GetAccessibleFoodTokenIds(board)
                : board.GetActiveFoodTokenIds();

            return TrySelectMostCommonFood(
                foodIds,
                availabilityByFoodId,
                random,
                out selected);
        }

        private static bool TrySelectNearestTrayFood(
            BoardModel board,
            IReadOnlyDictionary<int, FoodAvailability> availabilityByFoodId,
            PackageRandom random,
            out FoodAvailability selected)
        {
            selected = null;

            if (board.LayoutType == GrillLayoutType.StackedColumns)
            {
                return false;
            }

            for (int depth = 1; depth < board.FoodDepthCount; depth++)
            {
                if (TrySelectMostCommonFood(
                        board.GetFoodTokenIdsAtDepth(depth),
                        availabilityByFoodId,
                        random,
                        out selected))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TrySelectMostCommonFood(
            IReadOnlyList<int> foodIds,
            IReadOnlyDictionary<int, FoodAvailability> availabilityByFoodId,
            PackageRandom random,
            out FoodAvailability selected)
        {
            selected = null;
            Dictionary<int, int> foodCounts = CountAvailableFood(foodIds, availabilityByFoodId);

            if (foodCounts.Count == 0)
            {
                return false;
            }

            int highestCount = 0;
            List<int> mostCommonFoodIds = new();

            foreach (KeyValuePair<int, int> foodCount in foodCounts)
            {
                if (foodCount.Value < highestCount)
                {
                    continue;
                }

                if (foodCount.Value > highestCount)
                {
                    highestCount = foodCount.Value;
                    mostCommonFoodIds.Clear();
                }

                mostCommonFoodIds.Add(foodCount.Key);
            }

            mostCommonFoodIds.Sort();
            int selectedFoodId =
                mostCommonFoodIds[random.NextIndex(mostCommonFoodIds.Count)];
            selected = availabilityByFoodId[selectedFoodId];
            return true;
        }

        private static IReadOnlyList<int> GetAccessibleFoodTokenIds(BoardModel board)
        {
            List<int> foodIds = new();

            for (int grillIndex = 0; grillIndex < board.GrillCount; grillIndex++)
            {
                GrillModel grill = board.GetGrillAt(grillIndex);

                if (!board.IsGrillInActiveRows(grill.PositionIndex))
                {
                    continue;
                }

                for (int slotIndex = 0;
                     slotIndex < grill.ActiveFoodSlotCount;
                     slotIndex++)
                {
                    int foodId = grill.GetFoodTokenIdAt(slotIndex);

                    if (foodId > BoardRules.EmptyFoodTokenId)
                    {
                        foodIds.Add(foodId);
                    }
                }
            }

            return foodIds;
        }

        private static Dictionary<int, int> CountAvailableFood(
            IReadOnlyList<int> foodIds,
            IReadOnlyDictionary<int, FoodAvailability> availabilityByFoodId)
        {
            Dictionary<int, int> foodCounts = new();

            for (int i = 0; i < foodIds.Count; i++)
            {
                int foodId = foodIds[i];

                if (!availabilityByFoodId.ContainsKey(foodId))
                {
                    continue;
                }

                foodCounts.TryGetValue(foodId, out int count);
                foodCounts[foodId] = count + 1;
            }

            return foodCounts;
        }

        private static FoodAvailability SelectRandomCandidate(
            List<FoodAvailability> candidates,
            PackageRandom random)
        {
            candidates.Sort((left, right) => left.FoodId.CompareTo(right.FoodId));
            return candidates[random.NextIndex(candidates.Count)];
        }

        public bool TryCreateWaitingRackRescuePackage(
            BoardModel board,
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packageReservations,
            PackageRandom random,
            out RequiredPackageModel package)
        {
            package = null;

            if (board == null ||
                waitingRack == null ||
                packageReservations == null ||
                random == null)
            {
                return false;
            }

            HashSet<int> reservedFoodIds =
                GetReservedFoodIds(packageReservations);

            Dictionary<int, int> rackCounts = new();

            for (int i = 0; i < waitingRack.Capacity; i++)
            {
                int foodId = waitingRack.GetFoodTokenIdAt(i);

                if (foodId <= BoardRules.EmptyFoodTokenId ||
                    reservedFoodIds.Contains(foodId) ||
                    HasBlockedStackedGrillFood(board, foodId))
                {
                    continue;
                }

                rackCounts.TryGetValue(foodId, out int count);
                rackCounts[foodId] = count + 1;
            }

            if (rackCounts.Count == 0)
            {
                return false;
            }

            int highestCount = 0;
            List<int> bestFoodIds = new();

            foreach (KeyValuePair<int, int> pair in rackCounts)
            {
                if (pair.Value < highestCount)
                {
                    continue;
                }

                if (pair.Value > highestCount)
                {
                    highestCount = pair.Value;
                    bestFoodIds.Clear();
                }

                bestFoodIds.Add(pair.Key);
            }

            bestFoodIds.Sort();
            int selectedFoodId =
                bestFoodIds[random.NextIndex(bestFoodIds.Count)];

            package = new RequiredPackageModel(
                selectedFoodId,
                highestCount,
                filledAmount: 0);

            return true;
        }

        private static bool HasBlockedStackedGrillFood(
            BoardModel board,
            int foodId)
        {
            if (board.LayoutType != GrillLayoutType.StackedColumns)
            {
                return false;
            }

            for (int grillIndex = 0; grillIndex < board.GrillCount; grillIndex++)
            {
                GrillModel grill = board.GetGrillAt(grillIndex);

                if (board.IsGrillInActiveRows(grill.PositionIndex))
                {
                    continue;
                }

                for (int slotIndex = 0;
                     slotIndex < grill.ActiveFoodSlotCount;
                     slotIndex++)
                {
                    if (grill.GetFoodTokenIdAt(slotIndex) == foodId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private enum FoodLocation
        {
            WaitingRack,
            Grill,
            Fridge,
            TopTray,
            DeepTray
        }

        private sealed class FoodAvailability
        {
            public FoodAvailability(int foodId)
            {
                FoodId = foodId;
            }

            public int FoodId { get; }
            public int TotalCount { get; private set; }
            public int WaitingRackCount { get; private set; }
            public int GrillCount { get; private set; }
            public int FridgeCount { get; private set; }
            public int TopTrayCount { get; private set; }
            public int DeepTrayCount { get; private set; }
            public bool IsFullyRevealed => DeepTrayCount == 0;

            public void Add(FoodLocation location)
            {
                TotalCount++;

                switch (location)
                {
                    case FoodLocation.WaitingRack:
                        WaitingRackCount++;
                        break;
                    case FoodLocation.Grill:
                        GrillCount++;
                        break;
                    case FoodLocation.Fridge:
                        FridgeCount++;
                        break;
                    case FoodLocation.TopTray:
                        TopTrayCount++;
                        break;
                    case FoodLocation.DeepTray:
                        DeepTrayCount++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(location));
                }
            }
        }

        private sealed class FoodCandidateGroup
        {
            public FoodCandidateGroup(List<FoodAvailability> candidates, int weight)
            {
                Candidates = candidates;
                Weight = weight;
            }

            public List<FoodAvailability> Candidates { get; }
            public int Weight { get; }
        }
    }
}
