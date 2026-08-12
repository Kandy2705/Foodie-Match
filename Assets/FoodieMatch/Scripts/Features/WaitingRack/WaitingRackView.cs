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

        [Header("Intro Motion")]
        [SerializeField] private Transform _introOrigin;
        [SerializeField] private float _introDuration = 0.45f;
        [SerializeField] private float _introStagger = 0.1f;
        [SerializeField] private float _introArcHeight = 1f;
        [SerializeField] private float _introPeakProgress = 0.5f;
        [SerializeField] private Ease _introScaleEase = Ease.OutCubic;

        [Header("Add Slot Motion")]
        [SerializeField] private float _addSlotDuration = 0.35f;
        [SerializeField] private float _newSlotEnterOffset = 2.5f;
        [SerializeField] private Ease _addSlotEase = Ease.OutCubic;

        private readonly List<WaitingRackSlotView> _slots = new();
        private readonly HashSet<WaitingRackSlotView> _runtimeSlots = new();
        private readonly List<IntroSlotMotion> _introMotions = new();
        private FoodItemViewPool _foodItemViewPool;
        private Sequence _addSlotSequence;
        private Sequence _introSequence;
        private bool _isAddSlotAnimating;
        private bool _didIntroFinish;

        public int Capacity => _slots.Count;
        public bool IsAddSlotAnimating => _isAddSlotAnimating;
        public bool IsAtMaxCapacity => Capacity >= WaitingRackRules.MaxCapacity;

        private void Awake()
        {
            BuildInitialSlotList();
            LayoutSlotsImmediately();
        }

        private void OnDestroy()
        {
            StopIntroMotion();
            StopAddSlotMotion();
        }

        public void Construct(FoodItemViewPool foodItemViewPool)
        {
            _foodItemViewPool = foodItemViewPool;
        }

        public void ResetToCapacity(int capacity)
        {
            StopIntroMotion();
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

        public void PrepareIntro()
        {
            StopIntroMotion();
            _didIntroFinish = false;
            Vector3 introPosition = _introOrigin.position;

            for (int slotIndex = _slots.Count - 1; slotIndex >= 0; slotIndex--)
            {
                IntroSlotMotion motion = new(
                    _slots[slotIndex].transform,
                    introPosition,
                    _introArcHeight,
                    _introPeakProgress,
                    _introScaleEase);
                _introMotions.Add(motion);
            }
        }

        public async Task<MotionResult> PlayIntroAsync()
        {
            if (_introMotions.Count != _slots.Count)
            {
                return MotionResult.Failed;
            }

            _introSequence = Sequence.Create();

            for (int launchIndex = 0; launchIndex < _introMotions.Count; launchIndex++)
            {
                IntroSlotMotion motion = _introMotions[launchIndex];
                _ = _introSequence.Group(Tween.Custom(
                    motion,
                    0f,
                    1f,
                    _introDuration,
                    (slotMotion, progress) => slotMotion.Update(progress),
                    startDelay: launchIndex * _introStagger));
            }

            _ = _introSequence.ChainCallback(MarkIntroFinished);

            try
            {
                await _introSequence;
                return _didIntroFinish
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                RestoreIntroSlots();
                _introSequence = default;
            }
        }

        public void StopIntroMotion()
        {
            if (_introSequence.isAlive)
            {
                _introSequence.Stop();
            }

            RestoreIntroSlots();
            _introSequence = default;
        }

        public bool CanAddSlot()
        {
            return !_isAddSlotAnimating &&
                   !IsAtMaxCapacity;
        }

        public async Task<MotionResult> PlayAddSlotAsync()
        {
            if (!CanAddSlot())
            {
                return MotionResult.Failed;
            }

            WaitingRackSlotView newSlot = Instantiate(_slotPrefab, _slotRoot);
            newSlot.gameObject.name = $"{_slotPrefab.name}_{_slots.Count}";
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

        public Vector3 GetFoodAnchorWorldPositionAt(int index)
        {
            return GetSlot(index).FoodAnchorWorldPosition;
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                {
                    FoodItemView foodItemView =
                        _slots[i].RemoveFoodForReset();

                    if (foodItemView != null)
                    {
                        _foodItemViewPool.Release(foodItemView);
                    }
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

        private void MarkIntroFinished()
        {
            _didIntroFinish = true;
        }

        private void RestoreIntroSlots()
        {
            for (int i = 0; i < _introMotions.Count; i++)
            {
                _introMotions[i].Restore();
            }

            _introMotions.Clear();
        }

        private WaitingRackSlotView GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return null;
            }

            return _slots[index];
        }

        private sealed class IntroSlotMotion
        {
            private readonly Transform _slot;
            private readonly Vector3 _startPosition;
            private readonly Vector3 _targetPosition;
            private readonly Vector3 _targetScale;
            private readonly float _arcHeight;
            private readonly float _peakProgress;
            private readonly Ease _scaleEase;

            public IntroSlotMotion(
                Transform slot,
                Vector3 startPosition,
                float arcHeight,
                float peakProgress,
                Ease scaleEase)
            {
                _slot = slot;
                _startPosition = startPosition;
                _targetPosition = slot.position;
                _targetScale = slot.localScale;
                _arcHeight = arcHeight;
                _peakProgress = peakProgress;
                _scaleEase = scaleEase;

                _slot.position = _startPosition;
                _slot.localScale = Vector3.zero;
            }

            public void Update(float progress)
            {
                Vector3 position = Vector3.LerpUnclamped(
                    _startPosition,
                    _targetPosition,
                    progress);
                position.y = CalculatePositionY(progress);
                _slot.position = position;

                float scaleProgress = Easing.Evaluate(
                    progress,
                    _scaleEase);
                _slot.localScale = Vector3.LerpUnclamped(
                    Vector3.zero,
                    _targetScale,
                    scaleProgress);
            }

            public void Restore()
            {
                _slot.position = _targetPosition;
                _slot.localScale = _targetScale;
            }

            private float CalculatePositionY(float progress)
            {
                float peakPositionY = Mathf.Max(
                    _startPosition.y,
                    _targetPosition.y) + _arcHeight;

                if (progress <= _peakProgress)
                {
                    float risingProgress = progress / _peakProgress;
                    float easedProgress = 1f -
                        (1f - risingProgress) *
                        (1f - risingProgress);
                    return Mathf.LerpUnclamped(
                        _startPosition.y,
                        peakPositionY,
                        easedProgress);
                }

                float fallingProgress =
                    (progress - _peakProgress) /
                    (1f - _peakProgress);
                return Mathf.LerpUnclamped(
                    peakPositionY,
                    _targetPosition.y,
                    fallingProgress * fallingProgress);
            }
        }
    }
}
