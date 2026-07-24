using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class GrillMovementGroupDefinition
    {
        private readonly ReadOnlyCollection<int> _grillIds;

        public GrillMovementGroupDefinition(
            GrillMovementDirection direction,
            IReadOnlyList<int> grillIds,
            float movementSpeed)
        {
            if (!Enum.IsDefined(typeof(GrillMovementDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            if (grillIds == null)
            {
                throw new ArgumentNullException(nameof(grillIds));
            }

            if (grillIds.Count < 2)
            {
                throw new ArgumentException(
                    "Grill movement group must contain at least two grill ids.",
                    nameof(grillIds));
            }

            if (movementSpeed <= 0f ||
                float.IsNaN(movementSpeed) ||
                float.IsInfinity(movementSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
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
                        $"Grill id {grillId} is duplicated in a movement group.",
                        nameof(grillIds));
                }

                copiedGrillIds.Add(grillId);
            }

            Direction = direction;
            MovementSpeed = movementSpeed;
            _grillIds = copiedGrillIds.AsReadOnly();
        }

        public GrillMovementDirection Direction { get; }
        public IReadOnlyList<int> GrillIds => _grillIds;
        public float MovementSpeed { get; }
    }
}
