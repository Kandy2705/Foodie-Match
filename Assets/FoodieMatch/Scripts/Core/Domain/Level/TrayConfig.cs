using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class TrayConfig
    {
        private readonly ReadOnlyCollection<int> _foodTokenIds;

        public TrayConfig(IReadOnlyList<int> foodTokenIds)
        {
            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            List<int> copiedFoodTokenIds = new List<int>(foodTokenIds);
            _foodTokenIds = copiedFoodTokenIds.AsReadOnly();
        }

        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;
    }
}
