namespace FoodieMatch.Core.Domain.WaitingRack
{
    public sealed class WaitingRackState
    {
        private readonly int[] _foodTokenIds;

        public WaitingRackState(int capacity)
        {
            int validCapacity = capacity > 0 ? capacity : 0;
            _foodTokenIds = new int[validCapacity];
        }

        public int Capacity => _foodTokenIds.Length;
        public bool HasEmptySlot => GetFirstEmptySlotIndex() >= 0;
        public bool IsFull => Capacity > 0 && !HasEmptySlot;

        public int OccupiedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _foodTokenIds.Length; i++)
                {
                    if (_foodTokenIds[i] > 0)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int GetFoodTokenIdAt(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return 0;
            }

            return _foodTokenIds[slotIndex];
        }

        public int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < _foodTokenIds.Length; i++)
            {
                if (_foodTokenIds[i] == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool CanPlace(int foodTokenId)
        {
            return foodTokenId > 0 && HasEmptySlot;
        }

        public bool TryPlaceFood(
            int foodTokenId,
            out int slotIndex)
        {
            slotIndex = -1;

            if (!CanPlace(foodTokenId))
            {
                return false;
            }

            slotIndex = GetFirstEmptySlotIndex();
            _foodTokenIds[slotIndex] = foodTokenId;

            return true;
        }

        public bool TryRemoveFoodAt(
            int slotIndex,
            out int foodTokenId)
        {
            foodTokenId = 0;

            if (!IsValidSlotIndex(slotIndex) || _foodTokenIds[slotIndex] == 0)
            {
                return false;
            }

            foodTokenId = _foodTokenIds[slotIndex];
            _foodTokenIds[slotIndex] = 0;

            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _foodTokenIds.Length; i++)
            {
                _foodTokenIds[i] = 0;
            }
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _foodTokenIds.Length;
        }
    }
}
