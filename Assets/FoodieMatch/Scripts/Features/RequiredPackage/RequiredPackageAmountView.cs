using System;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    [Serializable]
    public sealed class RequiredPackageAmountView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RequiredPackageSlotView[] _slots;

        public int SlotCount => _slots != null ? _slots.Length : 0;

        public void Show(Sprite sprite, int filledAmount)
        {
            SetRootActive(true);

            if (_slots == null)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].Show(sprite, i < filledAmount);
                }
            }
        }

        public void Hide()
        {
            if (_slots != null)
            {
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].Hide();
                    }
                }
            }

            SetRootActive(false);
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
            {
                _root.SetActive(isActive);
            }
        }
    }
}
