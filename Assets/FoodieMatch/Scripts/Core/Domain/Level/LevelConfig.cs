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
            RequiredPackageGenerationSettings requiredPackageGenerationSettings,
            IReadOnlyList<GrillConfig> grills)
        {
            if (requiredPackageGenerationSettings == null)
            {
                throw new ArgumentNullException(
                    nameof(requiredPackageGenerationSettings));
            }

            if (grills == null)
            {
                throw new ArgumentNullException(nameof(grills));
            }

            WaitingRackCapacity = waitingRackCapacity;
            MaxPackageSlotCount = maxPackageSlotCount;
            RequiredPackageGenerationSettings =
                requiredPackageGenerationSettings;

            List<GrillConfig> copiedGrills =
                new List<GrillConfig>(grills);
            _grills = copiedGrills.AsReadOnly();
        }

        public int WaitingRackCapacity { get; }
        public int MaxPackageSlotCount { get; }
        public RequiredPackageGenerationSettings
            RequiredPackageGenerationSettings { get; }
        public IReadOnlyList<GrillConfig> Grills => _grills;
    }
}
