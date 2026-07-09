using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class GrillConfig
    {
        private readonly ReadOnlyCollection<int> _initialFoodTokenIds;
        private readonly ReadOnlyCollection<TrayConfig> _trays;

        public GrillConfig(
            int positionIndex,
            IReadOnlyList<int> initialFoodTokenIds,
            IReadOnlyList<TrayConfig> trays)
        {
            if (initialFoodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(initialFoodTokenIds));
            }

            if (trays == null)
            {
                throw new ArgumentNullException(nameof(trays));
            }

            PositionIndex = positionIndex;

            List<int> copiedFoodTokenIds =
                new List<int>(initialFoodTokenIds);
            List<TrayConfig> copiedTrays =
                new List<TrayConfig>(trays);

            _initialFoodTokenIds = copiedFoodTokenIds.AsReadOnly();
            _trays = copiedTrays.AsReadOnly();
        }

        public int PositionIndex { get; }
        public IReadOnlyList<int> InitialFoodTokenIds => _initialFoodTokenIds;
        public IReadOnlyList<TrayConfig> Trays => _trays;
    }
}
