using System.Collections.Generic;

namespace FoodieMatch.Core.Domain.Fridge
{
    public sealed class FridgeInventoryModel
    {
        private readonly List<int> _foodTokenIds = new();

        public int Count => _foodTokenIds.Count;
        public bool IsEmpty => Count == 0;

        public void Store(int foodTokenId)
        {
            if (!IsValidFoodTokenId(foodTokenId))
            {
                return;
            }

            _foodTokenIds.Add(foodTokenId);
        }

        public bool HasToken(int foodTokenId)
        {
            return IsValidFoodTokenId(foodTokenId) &&
                   _foodTokenIds.Contains(foodTokenId);
        }

        public bool TryTake(int foodTokenId, out int takenTokenId)
        {
            takenTokenId = 0;

            if (!IsValidFoodTokenId(foodTokenId))
            {
                return false;
            }

            int index = _foodTokenIds.IndexOf(foodTokenId);

            if (index < 0)
            {
                return false;
            }

            takenTokenId = _foodTokenIds[index];
            _foodTokenIds.RemoveAt(index);
            return true;
        }

        public void Restore(int foodTokenId)
        {
            if (!IsValidFoodTokenId(foodTokenId))
            {
                return;
            }

            _foodTokenIds.Add(foodTokenId);
        }

        public IReadOnlyList<int> GetAllTokenIds()
        {
            return new List<int>(_foodTokenIds).AsReadOnly();
        }

        public void Clear()
        {
            _foodTokenIds.Clear();
        }

        private static bool IsValidFoodTokenId(int foodTokenId)
        {
            return foodTokenId > 0;
        }
    }
}
