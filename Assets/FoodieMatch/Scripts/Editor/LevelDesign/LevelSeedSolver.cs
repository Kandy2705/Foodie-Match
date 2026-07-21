using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelSeedSolver
    {
        private readonly LevelSeedSolverSettings _settings;
        private readonly LevelSimulationFactory _simulationFactory;
        private readonly LevelSimulationActionProvider _actionProvider = new();
        private readonly LevelSimulationKeyBuilder _keyBuilder = new();

        public LevelSeedSolver(LevelSeedSolverSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            RequiredPackageMatcher matcher = new();
            RequiredPackageLifecycleUseCase packageLifecycleUseCase = new(
                new RequiredPackageGenerator(),
                matcher);
            SelectFoodUseCase selectFoodUseCase = new(matcher);
            LevelSimulationResolver resolver = new(packageLifecycleUseCase);
            _simulationFactory = new LevelSimulationFactory(
                new BoardModelFactory(),
                packageLifecycleUseCase,
                selectFoodUseCase,
                resolver);
        }

        public LevelSeedSolverResult Solve(
            LevelDefinition level,
            int packageSeed)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            LevelSeedSearchContext context = new(_settings);

            if (!_simulationFactory.TryCreate(level, packageSeed, out LevelSimulation simulation))
            {
                return CreateResult(LevelSeedSolverStatus.Unsolvable, context);
            }

            int initialRackOccupancy = simulation.WaitingRack.OccupiedCount;
            bool solved = Search(simulation, context, initialRackOccupancy);
            LevelSeedSolverStatus status = solved
                ? LevelSeedSolverStatus.Solved
                : context.HasReachedLimit
                    ? LevelSeedSolverStatus.SearchLimitReached
                    : LevelSeedSolverStatus.Unsolvable;

            return CreateResult(status, context);
        }

        private bool Search(
            LevelSimulation simulation,
            LevelSeedSearchContext context,
            int maximumRackOccupancy)
        {
            if (simulation.IsSolved)
            {
                context.SetSolution(maximumRackOccupancy);
                return true;
            }

            string stateKey = _keyBuilder.Build(simulation);

            if (!context.TryVisit(stateKey))
            {
                return false;
            }

            List<FoodBoardAddress> actions = _actionProvider.GetActions(simulation);

            for (int i = 0; i < actions.Count && !context.HasReachedLimit; i++)
            {
                FoodBoardAddress action = actions[i];
                LevelSimulation nextSimulation = _simulationFactory.Clone(simulation);

                if (!nextSimulation.TrySelectFood(action))
                {
                    continue;
                }

                int nextMaximumRackOccupancy = Math.Max(
                    maximumRackOccupancy,
                    nextSimulation.WaitingRack.OccupiedCount);

                context.PushAction(action);

                if (Search(nextSimulation, context, nextMaximumRackOccupancy))
                {
                    return true;
                }

                context.PopAction();
            }

            return false;
        }

        private static LevelSeedSolverResult CreateResult(
            LevelSeedSolverStatus status,
            LevelSeedSearchContext context)
        {
            return new LevelSeedSolverResult(
                status,
                context.VisitedStateCount,
                context.MaximumRackOccupancy,
                context.Elapsed,
                context.Solution);
        }
    }
}
