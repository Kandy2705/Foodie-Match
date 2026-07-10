using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelConfig
    {
        private readonly ReadOnlyCollection<GrillConfig> _grills;

        public LevelConfig(
            int waitingRackCapacity,
            int maxPackageSlotCount,
            IReadOnlyList<GrillConfig> grills)
        {
            if (grills == null)
            {
                throw new ArgumentNullException(nameof(grills));
            }

            WaitingRackCapacity = waitingRackCapacity;
            MaxPackageSlotCount = maxPackageSlotCount;

            List<GrillConfig> copiedGrills =
                new List<GrillConfig>(grills);
            _grills = copiedGrills.AsReadOnly();
        }

        public int WaitingRackCapacity { get; }
        public int MaxPackageSlotCount { get; }
        public IReadOnlyList<GrillConfig> Grills => _grills;
    }
}
