using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class BoardModelCloner
    {
        public BoardModel Clone(BoardModel source)
        {
            List<GrillModel> grills = new();

            for (int grillIndex = 0; grillIndex < source.GrillCount; grillIndex++)
            {
                grills.Add(CloneGrill(source.GetGrillAt(grillIndex)));
            }

            return new BoardModel(grills);
        }

        private static GrillModel CloneGrill(GrillModel source)
        {
            int[] activeFoodIds = CopyFoodSlots(
                source.ActiveFoodSlotCount,
                source.GetFoodTokenIdAt);
            bool isEmpty = source.ActiveFoodCount == 0;

            if (isEmpty)
            {
                activeFoodIds[0] = 1;
            }

            List<TrayModel> trays = new();

            for (int trayIndex = 0; trayIndex < source.TrayCount; trayIndex++)
            {
                TrayModel tray = source.GetTrayAt(trayIndex);
                int[] foodIds = CopyFoodSlots(tray.SlotCount, tray.GetFoodTokenIdAt);
                trays.Add(new TrayModel(foodIds));
            }

            GrillModel clone = new(
                source.PositionIndex,
                source.Position,
                activeFoodIds,
                trays);

            if (isEmpty &&
                !clone.TrySetFoodTokenIdAt(0, BoardRules.EmptyFoodTokenId))
            {
                throw new InvalidOperationException("Empty grill state could not be cloned.");
            }

            return clone;
        }

        private static int[] CopyFoodSlots(
            int slotCount,
            System.Func<int, int> getFoodId)
        {
            int[] foodIds = new int[slotCount];

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                foodIds[slotIndex] = getFoodId(slotIndex);
            }

            return foodIds;
        }
    }
}
