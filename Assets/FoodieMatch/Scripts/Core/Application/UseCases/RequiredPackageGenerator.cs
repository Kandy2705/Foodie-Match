using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Core.Application.UseCases
{
    public sealed class RequiredPackageGenerator
    {
        private readonly Random _random;

        public RequiredPackageGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool TryCreatePackage(
            BoardModel board,
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packages,
            RequiredPackageGenerationSettings settings,
            out RequiredPackageModel package)
        {
            package = null;

            if (board == null ||
                waitingRack == null ||
                packages == null ||
                settings == null)
            {
                return false;
            }

            Dictionary<int, int> availableAmounts =
                GetAvailableAmounts(
                    board,
                    waitingRack,
                    packages);

            List<FoodSourceCandidate> candidates =
                GetFoodSourceCandidates(
                    board,
                    waitingRack,
                    availableAmounts,
                    settings);

            if (!TrySelectFoodTokenId(
                    candidates,
                    out int foodTokenId) ||
                !availableAmounts.TryGetValue(
                    foodTokenId,
                    out int availableAmount) ||
                availableAmount <= 0)
            {
                return false;
            }

            int maxRequiredAmount = Math.Min(
                settings.MaxRequiredAmount,
                availableAmount);
            int minRequiredAmount = Math.Min(
                settings.MinRequiredAmount,
                maxRequiredAmount);
            int requiredAmount = _random.Next(
                minRequiredAmount,
                maxRequiredAmount + 1);

            package = new RequiredPackageModel(
                foodTokenId,
                requiredAmount,
                0);

            return true;
        }

        private static Dictionary<int, int> GetAvailableAmounts(
            BoardModel board,
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packages)
        {
            Dictionary<int, int> availableAmounts =
                new Dictionary<int, int>();

            AddFoodTokenIds(
                board.GetAllRemainingFoodTokenIds(),
                availableAmounts);
            AddFoodTokenIds(
                waitingRack.GetFoodTokenIds(),
                availableAmounts);

            for (int i = 0; i < packages.Count; i++)
            {
                RequiredPackageModel package = packages[i];

                if (package == null || package.IsComplete)
                {
                    continue;
                }

                if (!availableAmounts.TryGetValue(
                        package.FoodTokenId,
                        out int availableAmount))
                {
                    availableAmounts[package.FoodTokenId] =
                        -package.RemainingAmount;
                    continue;
                }

                availableAmounts[package.FoodTokenId] =
                    availableAmount - package.RemainingAmount;
            }

            return availableAmounts;
        }

        private static void AddFoodTokenIds(
            IReadOnlyList<int> foodTokenIds,
            Dictionary<int, int> amounts)
        {
            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (!amounts.TryGetValue(
                        foodTokenId,
                        out int amount))
                {
                    amounts.Add(foodTokenId, 1);
                    continue;
                }

                amounts[foodTokenId] = amount + 1;
            }
        }

        private static List<FoodSourceCandidate> GetFoodSourceCandidates(
            BoardModel board,
            WaitingRackModel waitingRack,
            IReadOnlyDictionary<int, int> availableAmounts,
            RequiredPackageGenerationSettings settings)
        {
            List<FoodSourceCandidate> candidates =
                new List<FoodSourceCandidate>
                {
                    new FoodSourceCandidate(
                        FilterAvailableFoodTokenIds(
                            waitingRack.GetFoodTokenIds(),
                            availableAmounts),
                        settings.WaitingRackWeight),
                    new FoodSourceCandidate(
                        FilterAvailableFoodTokenIds(
                            board.GetActiveFoodTokenIds(),
                            availableAmounts),
                        settings.ActiveGrillWeight),
                    new FoodSourceCandidate(
                        FilterAvailableFoodTokenIds(
                            board.GetTopTrayFoodTokenIds(),
                            availableAmounts),
                        settings.TopTrayWeight),
                    new FoodSourceCandidate(
                        FilterAvailableFoodTokenIds(
                            board.GetDeepTrayFoodTokenIds(),
                            availableAmounts),
                        settings.DeepTrayWeight)
                };

            return candidates;
        }

        private static List<int> FilterAvailableFoodTokenIds(
            IReadOnlyList<int> foodTokenIds,
            IReadOnlyDictionary<int, int> availableAmounts)
        {
            List<int> result = new List<int>();

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (availableAmounts.TryGetValue(
                        foodTokenId,
                        out int availableAmount) &&
                    availableAmount > 0)
                {
                    result.Add(foodTokenId);
                }
            }

            return result;
        }

        private bool TrySelectFoodTokenId(
            IReadOnlyList<FoodSourceCandidate> candidates,
            out int foodTokenId)
        {
            foodTokenId = 0;

            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                FoodSourceCandidate candidate = candidates[i];

                if (candidate.Weight > 0 &&
                    candidate.FoodTokenIds.Count > 0)
                {
                    totalWeight += candidate.Weight;
                }
            }

            if (totalWeight > 0)
            {
                int selectedWeight = _random.Next(totalWeight);

                for (int i = 0; i < candidates.Count; i++)
                {
                    FoodSourceCandidate candidate = candidates[i];

                    if (candidate.Weight <= 0 ||
                        candidate.FoodTokenIds.Count == 0)
                    {
                        continue;
                    }

                    if (selectedWeight < candidate.Weight)
                    {
                        foodTokenId = candidate.FoodTokenIds[
                            _random.Next(candidate.FoodTokenIds.Count)];

                        return true;
                    }

                    selectedWeight -= candidate.Weight;
                }
            }

            List<int> fallbackFoodTokenIds = new List<int>();

            for (int i = 0; i < candidates.Count; i++)
            {
                FoodSourceCandidate candidate = candidates[i];

                for (int tokenIndex = 0;
                     tokenIndex < candidate.FoodTokenIds.Count;
                     tokenIndex++)
                {
                    fallbackFoodTokenIds.Add(
                        candidate.FoodTokenIds[tokenIndex]);
                }
            }

            if (fallbackFoodTokenIds.Count == 0)
            {
                return false;
            }

            foodTokenId = fallbackFoodTokenIds[
                _random.Next(fallbackFoodTokenIds.Count)];

            return true;
        }

        private readonly struct FoodSourceCandidate
        {
            public FoodSourceCandidate(
                IReadOnlyList<int> foodTokenIds,
                int weight)
            {
                FoodTokenIds = foodTokenIds;
                Weight = weight;
            }

            public IReadOnlyList<int> FoodTokenIds { get; }
            public int Weight { get; }
        }
    }
}
