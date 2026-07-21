using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelSeedSolverResult
    {
        private readonly ReadOnlyCollection<FoodBoardAddress> _solution;

        public LevelSeedSolverResult(
            LevelSeedSolverStatus status,
            int visitedStateCount,
            int maximumRackOccupancy,
            TimeSpan elapsed,
            IReadOnlyList<FoodBoardAddress> solution)
        {
            Status = status;
            VisitedStateCount = visitedStateCount;
            MaximumRackOccupancy = maximumRackOccupancy;
            Elapsed = elapsed;

            List<FoodBoardAddress> copiedSolution = solution == null
                ? new List<FoodBoardAddress>()
                : new List<FoodBoardAddress>(solution);

            _solution = copiedSolution.AsReadOnly();
        }

        public LevelSeedSolverStatus Status { get; }
        public int VisitedStateCount { get; }
        public int MaximumRackOccupancy { get; }
        public TimeSpan Elapsed { get; }
        public IReadOnlyList<FoodBoardAddress> Solution => _solution;
    }
}
