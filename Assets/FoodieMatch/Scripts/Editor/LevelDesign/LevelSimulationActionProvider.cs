using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.RequiredPackage;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulationActionProvider
    {
        public List<FoodBoardAddress> GetActions(LevelSimulation simulation)
        {
            List<FoodBoardAddress> actions = new();

            for (int grillIndex = 0; grillIndex < simulation.Board.GrillCount; grillIndex++)
            {
                GrillModel grill = simulation.Board.GetGrillAt(grillIndex);

                for (int slotIndex = 0; slotIndex < grill.ActiveFoodSlotCount; slotIndex++)
                {
                    if (grill.GetFoodTokenIdAt(slotIndex) > BoardRules.EmptyFoodTokenId)
                    {
                        actions.Add(new FoodBoardAddress(grill.PositionIndex, slotIndex));
                    }
                }
            }

            actions.Sort((left, right) => CompareActions(simulation, left, right));
            return actions;
        }

        private static int CompareActions(
            LevelSimulation simulation,
            FoodBoardAddress left,
            FoodBoardAddress right)
        {
            int scoreComparison = GetScore(simulation, right).CompareTo(GetScore(simulation, left));

            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            int grillComparison = left.GrillPositionIndex.CompareTo(right.GrillPositionIndex);
            return grillComparison != 0
                ? grillComparison
                : left.FoodSlotIndex.CompareTo(right.FoodSlotIndex);
        }

        private static int GetScore(
            LevelSimulation simulation,
            FoodBoardAddress address)
        {
            GrillModel grill = GetGrill(simulation, address.GrillPositionIndex);
            int foodId = grill?.GetFoodTokenIdAt(address.FoodSlotIndex) ?? 0;
            int score = CanMatchPackage(simulation.RequiredPackages, foodId) ? 1000 : 0;

            if (grill != null && grill.ActiveFoodCount == 1 && grill.HasTrays)
            {
                score += 100;
            }

            return score;
        }

        private static GrillModel GetGrill(
            LevelSimulation simulation,
            int grillPositionIndex)
        {
            return simulation.Board.TryGetGrill(grillPositionIndex, out GrillModel grill)
                ? grill
                : null;
        }

        private static bool CanMatchPackage(
            IReadOnlyList<RequiredPackageModel> packages,
            int foodId)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i]?.CanAccept(foodId) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
