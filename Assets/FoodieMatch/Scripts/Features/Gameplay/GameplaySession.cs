using System;
using FoodieMatch.Core.Application.GameState;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Fridge;
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
            ComboProgressModel combo,
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
            RequiredPackages = requiredPackages ?? throw new ArgumentNullException(nameof(requiredPackages));
            WaitingRack = waitingRack ?? throw new ArgumentNullException(nameof(waitingRack));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Combo = combo ?? throw new ArgumentNullException(nameof(combo));
            PackageSettings = packageSettings ?? throw new ArgumentNullException(nameof(packageSettings));
        }

        public int SessionId { get; }
        public int LevelNumber { get; }
        public BoardModel Board { get; }
        public RequiredPackageModel[] RequiredPackages { get; }
        public WaitingRackModel WaitingRack { get; }
        public LevelProgressModel Progress { get; }
        public ComboProgressModel Combo { get; }
        public RequiredPackageGenerationSettings PackageSettings { get; }
        public FridgeInventoryModel FridgeInventory { get; private set; }
        public bool HasActivatedFridgeBooster => FridgeInventory != null;
        public int DisplayedServedCount { get; private set; }
        public LevelSessionState State { get; private set; }
        public bool IsInputEnabled { get; private set; }
        public bool IsDisplayedProgressUpToDate => DisplayedServedCount >= Progress.ServedCount;
        public bool CanSelectFood => State == LevelSessionState.Playing && IsInputEnabled;
        public bool CanContinueGameplay => State == LevelSessionState.Playing;

        public void StartPlaying()
        {
            State = LevelSessionState.Playing;
            IsInputEnabled = true;
        }

        public void DisableInput()
        {
            IsInputEnabled = false;
        }

        public bool TryActivateFridgeInventory(out FridgeInventoryModel inventory)
        {
            if (FridgeInventory != null)
            {
                inventory = FridgeInventory;
                return false;
            }

            FridgeInventory = new FridgeInventoryModel();
            inventory = FridgeInventory;
            return true;
        }

        public void ClearFridgeInventory()
        {
            FridgeInventory?.Clear();
            FridgeInventory = null;
        }

        public bool TryEnterAwaitingRevive()
        {
            if (State != LevelSessionState.Playing)
            {
                return false;
            }

            State = LevelSessionState.AwaitingRevive;
            IsInputEnabled = false;
            return true;
        }

        public bool TryMarkAsWon()
        {
            if (State != LevelSessionState.Playing)
            {
                return false;
            }

            State = LevelSessionState.Won;
            IsInputEnabled = false;
            return true;
        }

        public bool TryMarkAsLost()
        {
            if (State != LevelSessionState.AwaitingRevive)
            {
                return false;
            }

            State = LevelSessionState.Lost;
            IsInputEnabled = false;
            return true;
        }

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
