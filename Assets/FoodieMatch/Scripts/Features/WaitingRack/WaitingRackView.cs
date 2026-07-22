using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private WaitingRackSlotView[] _initialSlots;
        [SerializeField] private WaitingRackSlotView _slotPrefab;
        [SerializeField] private Transform _slotRoot;

        [Header("Layout")]
        [SerializeField] private float _slotSpacing = 1.3f;

        [Header("Add Slot Motion")]
        [SerializeField] private float _addSlotDuration = 0.35f;
        [SerializeField] private float _newSlotEnterOffset = 2.5f;
        [SerializeField] private Ease _addSlotEase = Ease.OutCubic;

        private readonly List<WaitingRackSlotView> _slots = new();
        private readonly HashSet<WaitingRackSlotView> _runtimeSlots = new();
        private Sequence _addSlotSequence;
        private bool _isAddSlotAnimating;

        public int Capacity => _slots.Count;
        public bool IsAddSlotAnimating => _isAddSlotAnimating;
        public bool IsAtMaxCapacity => Capacity >= WaitingRackRules.MaxCapacity;

        private void Awake()
        {
            if (_slotRoot == null)
            {
                _slotRoot = transform;
            }

            BuildInitialSlotList();
            LayoutSlotsImmediately();
        }

        private void OnDestroy()
        {
            StopAddSlotMotion();
            Clear();
        }

        public void ResetToCapacity(int capacity)
        {
            StopAddSlotMotion();
            Clear();
            RemoveRuntimeSlots();
            BuildInitialSlotList();

            if (capacity < _slots.Count)
            {
                for (int i = _slots.Count - 1; i >= capacity; i--)
                {
                    WaitingRackSlotView slot = _slots[i];

                    if (slot != null)
                    {
                        slot.gameObject.SetActive(false);
                    }

                    _slots.RemoveAt(i);
                }
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].gameObject.SetActive(true);
                }
            }

            LayoutSlotsImmediately();
        }

        public bool CanAddSlot()
        {
            return !_isAddSlotAnimating &&
                   !IsAtMaxCapacity &&
                   _slotPrefab != null &&
                   _slotRoot != null;
        }

        public async Task<MotionResult> PlayAddSlotAsync()
        {
            if (!CanAddSlot())
            {
                return MotionResult.Failed;
            }

            WaitingRackSlotView newSlot = Instantiate(_slotPrefab, _slotRoot);
            newSlot.gameObject.name = $"{_slotPrefab.name}_{_slots.Count}";
            newSlot.Clear();
            _runtimeSlots.Add(newSlot);
            _slots.Add(newSlot);

            int count = _slots.Count;
            Vector3[] targetPositions = BuildCenteredLocalPositions(count);

            Vector3 newSlotStart = targetPositions[count - 1] +
                                   Vector3.right * _newSlotEnterOffset;
            newSlot.transform.localPosition = newSlotStart;

            if (_addSlotDuration <= 0f ||
                float.IsNaN(_addSlotDuration) ||
                float.IsInfinity(_addSlotDuration))
            {
                ApplyLocalPositions(targetPositions);
                return MotionResult.Completed;
            }

            _isAddSlotAnimating = true;
            StopAddSlotMotion();

            try
            {
                Sequence sequence = Sequence.Create();

                for (int i = 0; i < count - 1; i++)
                {
                    WaitingRackSlotView slot = _slots[i];

                    if (slot == null)
                    {
                        continue;
                    }

                    _ = sequence.Group(Tween.LocalPosition(
                        slot.transform,
                        targetPositions[i],
                        _addSlotDuration,
                        _addSlotEase));
                }

                _ = sequence.Group(Tween.LocalPosition(
                    newSlot.transform,
                    targetPositions[count - 1],
                    _addSlotDuration,
                    _addSlotEase));

                _addSlotSequence = sequence;
                await sequence;
                return MotionResult.Completed;
            }
            finally
            {
                _addSlotSequence = default;
                _isAddSlotAnimating = false;
                ApplyLocalPositions(targetPositions);
            }
        }

        public bool RestoreFoodAt(int index, FoodItemView foodItemView)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.RestoreFood(foodItemView);
        }

        public bool TryReserveFoodAt(
            int index,
            FoodItemView foodItemView,
            out Vector3 targetPosition)
        {
            targetPosition = default;
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.TryReserveFood(
                foodItemView,
                out targetPosition);
        }

        public bool CompleteFoodPlacementAt(
            int index,
            FoodItemView expectedFoodItem)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.CompletePlacement(expectedFoodItem);
        }

        public bool PrepareFoodLandingAt(
            int index,
            FoodItemView expectedFoodItem)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.PrepareLanding(expectedFoodItem);
        }
        public bool TryGetFoodAt(
            int index,
            out FoodItemView foodItemView)
        {
            foodItemView = null;

            WaitingRackSlotView slot =
                GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning(
                    $"Waiting rack slot {index} is missing.",
                    this);

                return false;
            }

            return slot.TryGetFood(
                out foodItemView);
        }

        public FoodItemView RemoveFoodAt(int index)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return null;
            }

            return slot.RemoveFood();
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].Clear();
                }
            }
        }

        private void BuildInitialSlotList()
        {
            _slots.Clear();

            if (_initialSlots == null)
            {
                return;
            }

            for (int i = 0; i < _initialSlots.Length; i++)
            {
                if (_initialSlots[i] != null)
                {
                    _slots.Add(_initialSlots[i]);
                }
            }
        }

        private void RemoveRuntimeSlots()
        {
            foreach (WaitingRackSlotView slot in _runtimeSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _runtimeSlots.Clear();
        }

        private void LayoutSlotsImmediately()
        {
            if (_slots.Count == 0)
            {
                return;
            }

            ApplyLocalPositions(BuildCenteredLocalPositions(_slots.Count));
        }

        private Vector3[] BuildCenteredLocalPositions(int count)
        {
            Vector3[] positions = new Vector3[count];

            if (count <= 0)
            {
                return positions;
            }

            float startX = -(count - 1) * _slotSpacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(startX + i * _slotSpacing, 0f, 0f);
            }

            return positions;
        }

        private void ApplyLocalPositions(Vector3[] positions)
        {
            int count = Mathf.Min(_slots.Count, positions.Length);

            for (int i = 0; i < count; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].transform.localPosition = positions[i];
                }
            }
        }

        private void StopAddSlotMotion()
        {
            if (_addSlotSequence.isAlive)
            {
                _addSlotSequence.Stop();
            }

            _addSlotSequence = default;
        }

        private WaitingRackSlotView GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return null;
            }

            return _slots[index];
        }
    }
}
