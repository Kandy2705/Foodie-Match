using FoodieMatch.Core.Infrastructure.Save;
using UnityEngine;

namespace FoodieMatch.Data.Booster
{
    public sealed class BoosterManager
    {
        private const string SaveKeyPrefix = "Booster_";
        private static readonly BoosterType[] AllTypes = (BoosterType[])System.Enum.GetValues(typeof(BoosterType));

        private readonly ISaveService _saveService;
        private readonly int[] _defaultCounts;

        public BoosterManager(ISaveService saveService, int[] defaultCounts = null)
        {
            _saveService = saveService;
            _defaultCounts = defaultCounts ?? new int[] { 0, 0, 0, 0 };
        }

        public int GetCount(BoosterType type)
        {
            string key = GetSaveKey(type);
            int defaultCount = GetDefaultIndex((int)type);
            return _saveService.GetInt(key, defaultCount);
        }

        public int[] GetCounts()
        {
            int[] counts = new int[AllTypes.Length];
            for (int i = 0; i < AllTypes.Length; i++)
            {
                counts[i] = GetCount(AllTypes[i]);
            }
            return counts;
        }

        public void SetCount(BoosterType type, int count)
        {
            count = Mathf.Max(0, count);
            _saveService.SetInt(GetSaveKey(type), count);
            _saveService.Save();
        }

        public void Add(BoosterType type, int amount)
        {
            int current = GetCount(type);
            SetCount(type, current + amount);
        }

        public bool TryUse(BoosterType type)
        {
            int current = GetCount(type);
            if (current <= 0)
            {
                return false;
            }
            SetCount(type, current - 1);
            return true;
        }

        public bool HasCount(BoosterType type)
        {
            return GetCount(type) > 0;
        }

        public void ResetToDefaults()
        {
            for (int i = 0; i < AllTypes.Length; i++)
            {
                _saveService.SetInt(GetSaveKey(AllTypes[i]), _defaultCounts[i]);
            }
            _saveService.Save();
        }

        private string GetSaveKey(BoosterType type)
        {
            return SaveKeyPrefix + type.ToString();
        }

        private int GetDefaultIndex(int index)
        {
            if (_defaultCounts != null && index >= 0 && index < _defaultCounts.Length)
            {
                return _defaultCounts[index];
            }
            return 0;
        }
    }
}
