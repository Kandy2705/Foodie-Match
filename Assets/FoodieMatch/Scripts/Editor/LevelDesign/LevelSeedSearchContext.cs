using System.Collections.Generic;
using System.Diagnostics;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSeedSearchContext
    {
        private readonly LevelSeedSolverSettings _settings;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly HashSet<string> _visitedStates = new();
        private readonly List<FoodBoardAddress> _currentPath = new();

        private List<FoodBoardAddress> _solution;

        public LevelSeedSearchContext(LevelSeedSolverSettings settings)
        {
            _settings = settings;
        }

        public int VisitedStateCount => _visitedStates.Count;
        public bool HasReachedLimit { get; private set; }
        public int MaximumRackOccupancy { get; private set; }
        public IReadOnlyList<FoodBoardAddress> CurrentPath => _currentPath;
        public IReadOnlyList<FoodBoardAddress> Solution => _solution;
        public System.TimeSpan Elapsed => _stopwatch.Elapsed;

        public bool TryVisit(string stateKey)
        {
            if (_visitedStates.Count >= _settings.MaximumVisitedStates ||
                _stopwatch.Elapsed >= _settings.MaximumDuration)
            {
                HasReachedLimit = true;
                return false;
            }

            return _visitedStates.Add(stateKey);
        }

        public void PushAction(FoodBoardAddress action)
        {
            _currentPath.Add(action);
        }

        public void PopAction()
        {
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }

        public void SetSolution(int maximumRackOccupancy)
        {
            _solution = new List<FoodBoardAddress>(_currentPath);
            MaximumRackOccupancy = maximumRackOccupancy;
        }
    }
}
