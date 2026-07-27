using System;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    [Serializable]
    public sealed class RequiredPackageAmountView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RequiredPackageSlotView[] _slots;

        public int SlotCount => _slots.Length;

        public RequiredPackageSlotView GetSlotAt(int slotIndex)
        {
            return slotIndex >= 0 &&
                   slotIndex < _slots.Length
                ? _slots[slotIndex]
                : null;
        }

        public void Show(Sprite sprite, int filledAmount)
        {
            SetRootActive(true);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Show(sprite, i < filledAmount);
            }
        }

        public void Hide()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Hide();
            }

            SetRootActive(false);
        }

        private void SetRootActive(bool isActive)
        {
            _root.SetActive(isActive);
        }
    }
}
