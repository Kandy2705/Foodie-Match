using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Board
{
    public sealed class StackedGrillColumnState
    {
        private readonly List<int> _grillPositionIndices;
        private readonly ReadOnlyCollection<int> _readOnlyGrillPositionIndices;

        public StackedGrillColumnState(IReadOnlyList<int> grillPositionIndices)
        {
            if (grillPositionIndices == null)
            {
                throw new ArgumentNullException(nameof(grillPositionIndices));
            }

            _grillPositionIndices = new(grillPositionIndices);
            _readOnlyGrillPositionIndices = _grillPositionIndices.AsReadOnly();
        }

        public IReadOnlyList<int> GrillPositionIndices => _readOnlyGrillPositionIndices;

        public bool TryGetRowIndex(int grillPositionIndex, out int rowIndex)
        {
            rowIndex = _grillPositionIndices.IndexOf(grillPositionIndex);
            return rowIndex >= 0;
        }

        internal bool Remove(int grillPositionIndex)
        {
            return _grillPositionIndices.Remove(grillPositionIndex);
        }
    }
}
