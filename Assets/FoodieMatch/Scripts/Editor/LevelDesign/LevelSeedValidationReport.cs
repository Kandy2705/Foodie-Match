using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelSeedValidationReport
    {
        public string GeneratedAtUtc { get; set; }
        public List<LevelSeedValidationEntry> Results { get; set; } = new();
    }

    public sealed class LevelSeedValidationEntry
    {
        public int LevelId { get; set; }
        public int PackageSeed { get; set; }
        public string Status { get; set; }
        public int VisitedStateCount { get; set; }
        public int MaximumRackOccupancy { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public List<LevelSeedSolutionStep> Solution { get; set; } = new();

        public static LevelSeedValidationEntry Create(
            int levelId,
            int packageSeed,
            LevelSeedSolverResult result)
        {
            LevelSeedValidationEntry entry = new()
            {
                LevelId = levelId,
                PackageSeed = packageSeed,
                Status = result.Status.ToString(),
                VisitedStateCount = result.VisitedStateCount,
                MaximumRackOccupancy = result.MaximumRackOccupancy,
                ElapsedMilliseconds = result.Elapsed.TotalMilliseconds
            };

            for (int i = 0; i < result.Solution.Count; i++)
            {
                FoodBoardAddress address = result.Solution[i];
                entry.Solution.Add(
                    new LevelSeedSolutionStep
                    {
                        GrillPositionIndex = address.GrillPositionIndex,
                        FoodSlotIndex = address.FoodSlotIndex
                    });
            }

            return entry;
        }
    }

    public sealed class LevelSeedSolutionStep
    {
        public int GrillPositionIndex { get; set; }
        public int FoodSlotIndex { get; set; }
    }
}
