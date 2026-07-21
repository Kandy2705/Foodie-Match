using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulation
    {
        private readonly SelectFoodUseCase _selectFoodUseCase;
        private readonly LevelSimulationResolver _resolver;

        public LevelSimulation(
            LevelDefinition level,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] requiredPackages,
            PackageRandom packageRandom,
            int servedFoodCount,
            SelectFoodUseCase selectFoodUseCase,
            LevelSimulationResolver resolver)
        {
            Level = level;
            Board = board;
            WaitingRack = waitingRack;
            RequiredPackages = requiredPackages;
            PackageRandom = packageRandom;
            ServedFoodCount = servedFoodCount;
            _selectFoodUseCase = selectFoodUseCase;
            _resolver = resolver;
        }

        public LevelDefinition Level { get; }
        public BoardModel Board { get; }
        public WaitingRackModel WaitingRack { get; }
        public RequiredPackageModel[] RequiredPackages { get; }
        public PackageRandom PackageRandom { get; }
        public int ServedFoodCount { get; private set; }

        public bool IsSolved =>
            ServedFoodCount == Level.TotalFoodCount &&
            !Board.HasRemainingFood &&
            WaitingRack.OccupiedCount == 0;

        public bool TrySelectFood(FoodBoardAddress address)
        {
            SelectFoodResult result = _selectFoodUseCase.Execute(
                address,
                Board,
                RequiredPackages,
                WaitingRack);

            if (!result.IsPlaced)
            {
                return false;
            }

            if (result.Type == SelectFoodResultType.PlacedInRequiredPackage &&
                !TryServeFood())
            {
                return false;
            }

            Board.TryMoveTopTrayToGrill(address.GrillPositionIndex, out _);

            if (result.Type == SelectFoodResultType.PlacedInWaitingRack &&
                WaitingRack.IsFull)
            {
                return false;
            }

            return _resolver.TryResolve(this);
        }

        public bool TryServeFood()
        {
            if (ServedFoodCount >= Level.TotalFoodCount)
            {
                return false;
            }

            ServedFoodCount++;
            return true;
        }
    }
}
