using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Core.Domain.Grill
{
    public sealed class GrillModel
    {
        private readonly int[] _activeFoodTokenIds;
        private readonly List<TrayModel> _trays;

        public GrillModel(
            int positionIndex,
            IReadOnlyList<int> activeFoodTokenIds,
            IReadOnlyList<TrayModel> trays)
        {
            if (positionIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionIndex));
            }

            PositionIndex = positionIndex;
            _activeFoodTokenIds = CopyFoodSlots(activeFoodTokenIds);
            _trays = CopyTrays(trays);
        }

        public int PositionIndex { get; }
        public int ActiveFoodSlotCount => _activeFoodTokenIds.Length;
        public int TrayCount => _trays.Count;
        public bool IsEmpty => ActiveFoodCount == 0;
        public bool HasTrays => _trays.Count > 0;
        public bool HasRemainingFood => !IsEmpty || HasTrays;
        public TrayModel TopTray => HasTrays ? _trays[0] : null;

        public TrayModel GetTrayAt(int index)
        {
            return index >= 0 && index < _trays.Count
                ? _trays[index]
                : null;
        }

        public int ActiveFoodCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _activeFoodTokenIds.Length; i++)
                {
                    if (_activeFoodTokenIds[i] > BoardRules.EmptyFoodTokenId)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int RemainingFoodCount
        {
            get
            {
                int count = ActiveFoodCount;

                for (int i = 0; i < _trays.Count; i++)
                {
                    count += _trays[i].FoodCount;
                }

                return count;
            }
        }

        public int GetFoodTokenIdAt(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex)
                ? _activeFoodTokenIds[slotIndex]
                : BoardRules.EmptyFoodTokenId;
        }

        public bool CanRemoveFood(
            int slotIndex,
            int expectedFoodTokenId)
        {
            return expectedFoodTokenId > BoardRules.EmptyFoodTokenId &&
                   GetFoodTokenIdAt(slotIndex) == expectedFoodTokenId;
        }

        public bool TryRemoveFood(
            int slotIndex,
            int expectedFoodTokenId)
        {
            if (!CanRemoveFood(slotIndex, expectedFoodTokenId))
            {
                return false;
            }

            _activeFoodTokenIds[slotIndex] = BoardRules.EmptyFoodTokenId;
            return true;
        }

        public bool CanRestoreFood(
            int slotIndex,
            int foodTokenId)
        {
            return foodTokenId > BoardRules.EmptyFoodTokenId &&
                   IsValidSlotIndex(slotIndex) &&
                   _activeFoodTokenIds[slotIndex] ==
                       BoardRules.EmptyFoodTokenId;
        }

        public bool TryRestoreFood(
            int slotIndex,
            int foodTokenId)
        {
            if (!CanRestoreFood(slotIndex, foodTokenId))
            {
                return false;
            }

            _activeFoodTokenIds[slotIndex] = foodTokenId;
            return true;
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _activeFoodTokenIds.Length;
        }

        private static int[] CopyFoodSlots(
            IReadOnlyList<int> foodTokenIds)
        {
            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            if (foodTokenIds.Count < BoardRules.MinFoodSlotCount ||
                foodTokenIds.Count > BoardRules.MaxFoodSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(foodTokenIds));
            }

            int[] foodSlots = new int[foodTokenIds.Count];
            bool hasFood = false;

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                if (foodTokenIds[i] < BoardRules.EmptyFoodTokenId)
                {
                    throw new ArgumentOutOfRangeException(nameof(foodTokenIds));
                }

                foodSlots[i] = foodTokenIds[i];
                hasFood |= foodTokenIds[i] > BoardRules.EmptyFoodTokenId;
            }

            if (!hasFood)
            {
                throw new ArgumentException(
                    "Grill must contain at least one active food token.",
                    nameof(foodTokenIds));
            }

            return foodSlots;
        }

        private static List<TrayModel> CopyTrays(
            IReadOnlyList<TrayModel> trays)
        {
            List<TrayModel> trayModels = new List<TrayModel>();

            if (trays == null)
            {
                return trayModels;
            }

            for (int i = 0; i < trays.Count; i++)
            {
                if (trays[i] == null)
                {
                    throw new ArgumentException(
                        "Tray collection cannot contain null.",
                        nameof(trays));
                }

                trayModels.Add(trays[i]);
            }

            return trayModels;
        }
    }
}
