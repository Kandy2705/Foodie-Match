using System;
using FoodieMatch.Core.Application.GameState;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class GameplaySession
    {
        public GameplaySession(
            int sessionId,
            int levelNumber,
            BoardModel board,
            RequiredPackageModel[] requiredPackages,
            WaitingRackModel waitingRack,
            LevelProgressModel progress,
            RequiredPackageGenerationSettings packageSettings)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            if (levelNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            }

            SessionId = sessionId;
            LevelNumber = levelNumber;
            Board = board ?? throw new ArgumentNullException(nameof(board));
            RequiredPackages = requiredPackages ??
                throw new ArgumentNullException(nameof(requiredPackages));
            WaitingRack = waitingRack ??
                throw new ArgumentNullException(nameof(waitingRack));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            PackageSettings = packageSettings ??
                throw new ArgumentNullException(nameof(packageSettings));
        }

        public int SessionId { get; }
        public int LevelNumber { get; }
        public BoardModel Board { get; }
        public RequiredPackageModel[] RequiredPackages { get; }
        public WaitingRackModel WaitingRack { get; }
        public LevelProgressModel Progress { get; }
        public RequiredPackageGenerationSettings PackageSettings { get; }
        public int DisplayedServedCount { get; private set; }
        public bool IsDisplayedProgressUpToDate =>
            DisplayedServedCount >= Progress.ServedCount;

        public bool TryIncreaseDisplayedServedCount()
        {
            if (IsDisplayedProgressUpToDate)
            {
                return false;
            }

            DisplayedServedCount++;
            return true;
        }
    }
}
