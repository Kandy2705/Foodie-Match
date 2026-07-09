using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Grill;

namespace FoodieMatch.Core.Domain.Board
{
    public sealed class BoardModel
    {
        private readonly List<GrillModel> _grills;

        public BoardModel(IReadOnlyList<GrillModel> grills)
        {
            if (grills == null)
            {
                throw new ArgumentNullException(nameof(grills));
            }

            if (grills.Count == 0)
            {
                throw new ArgumentException(
                    "Board must contain at least one grill.",
                    nameof(grills));
            }

            _grills = CopyGrills(grills);
        }

        public int GrillCount => _grills.Count;

        public GrillModel GetGrillAt(int index)
        {
            return index >= 0 && index < _grills.Count
                ? _grills[index]
                : null;
        }

        public bool HasRemainingFood
        {
            get
            {
                for (int i = 0; i < _grills.Count; i++)
                {
                    if (_grills[i].HasRemainingFood)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int RemainingFoodCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _grills.Count; i++)
                {
                    count += _grills[i].RemainingFoodCount;
                }

                return count;
            }
        }

        public bool CanRemoveFood(
            FoodBoardAddress address,
            int expectedFoodTokenId)
        {
            return address.IsValid &&
                   TryGetGrill(
                       address.GrillPositionIndex,
                       out GrillModel grill) &&
                   grill.CanRemoveFood(
                       address.FoodSlotIndex,
                       expectedFoodTokenId);
        }

        public bool TryRemoveFood(
            FoodBoardAddress address,
            int expectedFoodTokenId)
        {
            if (!address.IsValid ||
                !TryGetGrill(
                    address.GrillPositionIndex,
                    out GrillModel grill))
            {
                return false;
            }

            return grill.TryRemoveFood(
                address.FoodSlotIndex,
                expectedFoodTokenId);
        }

        public int GetFoodTokenId(FoodBoardAddress address)
        {
            if (!address.IsValid ||
                !TryGetGrill(
                    address.GrillPositionIndex,
                    out GrillModel grill))
            {
                return BoardRules.EmptyFoodTokenId;
            }

            return grill.GetFoodTokenIdAt(address.FoodSlotIndex);
        }

        public bool TryRestoreFood(
            FoodBoardAddress address,
            int foodTokenId)
        {
            if (!address.IsValid ||
                !TryGetGrill(
                    address.GrillPositionIndex,
                    out GrillModel grill))
            {
                return false;
            }

            return grill.TryRestoreFood(
                address.FoodSlotIndex,
                foodTokenId);
        }

        public bool TryGetGrill(
            int positionIndex,
            out GrillModel grill)
        {
            grill = null;

            for (int i = 0; i < _grills.Count; i++)
            {
                if (_grills[i].PositionIndex == positionIndex)
                {
                    grill = _grills[i];
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<int> GetActiveFoodTokenIds()
        {
            List<int> foodTokenIds = new List<int>();

            for (int grillIndex = 0; grillIndex < _grills.Count; grillIndex++)
            {
                GrillModel grill = _grills[grillIndex];

                for (int slotIndex = 0;
                     slotIndex < grill.ActiveFoodSlotCount;
                     slotIndex++)
                {
                    int foodTokenId = grill.GetFoodTokenIdAt(slotIndex);

                    if (foodTokenId > BoardRules.EmptyFoodTokenId)
                    {
                        foodTokenIds.Add(foodTokenId);
                    }
                }
            }

            return foodTokenIds;
        }

        public IReadOnlyList<int> GetTopTrayFoodTokenIds()
        {
            List<int> foodTokenIds = new List<int>();

            for (int grillIndex = 0; grillIndex < _grills.Count; grillIndex++)
            {
                TrayModel topTray = _grills[grillIndex].TopTray;

                if (topTray == null)
                {
                    continue;
                }

                for (int slotIndex = 0;
                     slotIndex < topTray.SlotCount;
                     slotIndex++)
                {
                    int foodTokenId = topTray.GetFoodTokenIdAt(slotIndex);

                    if (foodTokenId > BoardRules.EmptyFoodTokenId)
                    {
                        foodTokenIds.Add(foodTokenId);
                    }
                }
            }

            return foodTokenIds;
        }

        private static List<GrillModel> CopyGrills(
            IReadOnlyList<GrillModel> grills)
        {
            List<GrillModel> grillModels = new List<GrillModel>();
            HashSet<int> positionIndexes = new HashSet<int>();

            for (int i = 0; i < grills.Count; i++)
            {
                GrillModel grill = grills[i];

                if (grill == null)
                {
                    throw new ArgumentException(
                        "Board grill collection cannot contain null.",
                        nameof(grills));
                }

                if (!positionIndexes.Add(grill.PositionIndex))
                {
                    throw new ArgumentException(
                        "Board grill position indexes must be unique.",
                        nameof(grills));
                }

                grillModels.Add(grill);
            }

            return grillModels;
        }
    }
}
