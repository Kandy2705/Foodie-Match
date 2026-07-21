using System.Text;
using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.RequiredPackage;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulationKeyBuilder
    {
        public string Build(LevelSimulation simulation)
        {
            StringBuilder key = new();
            PackageRandomState randomState = simulation.PackageRandom.CaptureState();
            key.Append(simulation.ServedFoodCount).Append('|');
            key.Append(randomState.Seed).Append(',').Append(randomState.DrawCount).Append('|');
            AppendPackages(key, simulation.RequiredPackages);
            AppendWaitingRack(key, simulation);
            AppendBoard(key, simulation);
            return key.ToString();
        }

        private static void AppendPackages(
            StringBuilder key,
            RequiredPackageModel[] packages)
        {
            key.Append('P');

            for (int i = 0; i < packages.Length; i++)
            {
                RequiredPackageModel package = packages[i];

                if (package == null)
                {
                    key.Append("n;");
                    continue;
                }

                key.Append(package.FoodTokenId).Append(',');
                key.Append(package.RequiredAmount).Append(',');
                key.Append(package.FilledAmount).Append(';');
            }
        }

        private static void AppendWaitingRack(
            StringBuilder key,
            LevelSimulation simulation)
        {
            key.Append("|R");

            for (int i = 0; i < simulation.WaitingRack.Capacity; i++)
            {
                key.Append(simulation.WaitingRack.GetFoodTokenIdAt(i)).Append(',');
            }
        }

        private static void AppendBoard(
            StringBuilder key,
            LevelSimulation simulation)
        {
            key.Append("|B");

            for (int grillIndex = 0; grillIndex < simulation.Board.GrillCount; grillIndex++)
            {
                GrillModel grill = simulation.Board.GetGrillAt(grillIndex);
                key.Append('G').Append(grill.PositionIndex).Append('[');
                AppendFoodSlots(key, grill.ActiveFoodSlotCount, grill.GetFoodTokenIdAt);
                key.Append(']');

                for (int trayIndex = 0; trayIndex < grill.TrayCount; trayIndex++)
                {
                    TrayModel tray = grill.GetTrayAt(trayIndex);
                    key.Append('T').Append('[');
                    AppendFoodSlots(key, tray.SlotCount, tray.GetFoodTokenIdAt);
                    key.Append(']');
                }

                key.Append(';');
            }
        }

        private static void AppendFoodSlots(
            StringBuilder key,
            int slotCount,
            System.Func<int, int> getFoodId)
        {
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                key.Append(getFoodId(slotIndex)).Append(',');
            }
        }
    }
}
