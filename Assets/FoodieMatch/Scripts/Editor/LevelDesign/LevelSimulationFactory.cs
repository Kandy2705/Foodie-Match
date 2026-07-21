using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulationFactory
    {
        private readonly BoardModelFactory _boardModelFactory;
        private readonly RequiredPackageLifecycleUseCase _packageLifecycleUseCase;
        private readonly SelectFoodUseCase _selectFoodUseCase;
        private readonly LevelSimulationResolver _resolver;
        private readonly LevelSimulationCloner _cloner;

        public LevelSimulationFactory(
            BoardModelFactory boardModelFactory,
            RequiredPackageLifecycleUseCase packageLifecycleUseCase,
            SelectFoodUseCase selectFoodUseCase,
            LevelSimulationResolver resolver)
        {
            _boardModelFactory = boardModelFactory;
            _packageLifecycleUseCase = packageLifecycleUseCase;
            _selectFoodUseCase = selectFoodUseCase;
            _resolver = resolver;
            _cloner = new LevelSimulationCloner(selectFoodUseCase, resolver);
        }

        public bool TryCreate(
            LevelDefinition level,
            int packageSeed,
            out LevelSimulation simulation)
        {
            BoardModel board = _boardModelFactory.Create(level);
            WaitingRackModel waitingRack = new(WaitingRackRules.InitialCapacity);
            PackageRandom packageRandom = new(packageSeed);

            if (!_packageLifecycleUseCase.TryCreateInitialPackages(
                    board,
                    waitingRack,
                    level.PackageSelectionSettings,
                    packageRandom,
                    out RequiredPackageModel[] requiredPackages))
            {
                simulation = null;
                return false;
            }

            simulation = CreateSimulation(
                level,
                board,
                waitingRack,
                requiredPackages,
                packageRandom,
                servedFoodCount: 0);

            return true;
        }

        public LevelSimulation Clone(LevelSimulation source)
        {
            return _cloner.Clone(source);
        }

        private LevelSimulation CreateSimulation(
            LevelDefinition level,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] requiredPackages,
            PackageRandom packageRandom,
            int servedFoodCount)
        {
            return new LevelSimulation(
                level,
                board,
                waitingRack,
                requiredPackages,
                packageRandom,
                servedFoodCount,
                _selectFoodUseCase,
                _resolver);
        }

    }
}
