using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class StackedGrillColumnDefinition
    {
        private readonly ReadOnlyCollection<int> _grillIds;

        public StackedGrillColumnDefinition(IReadOnlyList<int> grillIds)
        {
            if (grillIds == null)
            {
                throw new ArgumentNullException(nameof(grillIds));
            }

            if (grillIds.Count == 0)
            {
                throw new ArgumentException(
                    "Stacked grill column must contain at least one grill id.",
                    nameof(grillIds));
            }

            HashSet<int> uniqueGrillIds = new();
            List<int> copiedGrillIds = new(grillIds.Count);

            for (int i = 0; i < grillIds.Count; i++)
            {
                int grillId = grillIds[i];

                if (grillId <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(grillIds));
                }

                if (!uniqueGrillIds.Add(grillId))
                {
                    throw new ArgumentException(
                        $"Grill id {grillId} is duplicated in a stacked grill column.",
                        nameof(grillIds));
                }

                copiedGrillIds.Add(grillId);
            }

            _grillIds = copiedGrillIds.AsReadOnly();
        }

        public IReadOnlyList<int> GrillIds => _grillIds;
    }
}
