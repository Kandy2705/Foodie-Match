using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageGroupView : MonoBehaviour
    {
        [SerializeField] private RequiredPackageView[] _packages;

        [Header("Layout Motion")]
        [SerializeField] private float _layoutMotionDuration = 0.25f;
        [SerializeField] private Ease _layoutMotionEase = Ease.OutCubic;

        private readonly List<PackageLayoutItem> _layoutItems = new();

        private Sequence _layoutSequence;
        private TaskCompletionSource<MotionResult> _layoutCompletion;
        private int _layoutMotionId;
        private float _layoutCenterX;
        private float _layoutSpacing;
        private bool _isLayoutInitialized;

        public int PackageCount => _packages != null ? _packages.Length : 0;

        private void Awake()
        {
            InitializeLayout();
        }

        private void OnDestroy()
        {
            CancelLayoutMotion();
        }

        public RequiredPackageSlotView GetTargetSlot(
            int packageIndex,
            int requiredAmount,
            int filledSlotIndex)
        {
            RequiredPackageView packageView = GetPackageAt(packageIndex);
            return packageView?.GetTargetSlot(requiredAmount, filledSlotIndex);
        }

        public bool ShowPackageAt(int packageIndex, RequiredPackageModel package, Sprite sprite)
        {
            RequiredPackageView packageView = GetPackageAt(packageIndex);

            if (packageView == null)
            {
                return false;
            }

            if (package == null || package.IsEmpty)
            {
                packageView.Clear();
                packageView.gameObject.SetActive(false);
                return true;
            }

            packageView.gameObject.SetActive(true);
            packageView.Setup(package.FoodTokenId, package.RequiredAmount, sprite);
            packageView.SetFilledAmount(package.FilledAmount);

            return true;
        }

        public RequiredPackageView GetPackageAt(int packageIndex)
        {
            if (_packages == null || packageIndex < 0 || packageIndex >= _packages.Length)
            {
                return null;
            }

            return _packages[packageIndex];
        }

        public Task<MotionResult> RecenterVisibleItemsAsync()
        {
            if (!InitializeLayout() || !IsValidTime(_layoutMotionDuration))
            {
                return Task.FromResult(MotionResult.Failed);
            }

            List<PackageLayoutItem> visibleItems = GetVisibleLayoutItems();

            if (visibleItems.Count == 0)
            {
                CompleteLayoutImmediately();
                return Task.FromResult(MotionResult.Completed);
            }

            Vector3[] targetPositions = GetTargetPositions(visibleItems);

            if (_layoutMotionDuration == 0f)
            {
                ApplyTargetPositions(visibleItems, targetPositions);
                CompleteLayoutImmediately();
                return Task.FromResult(MotionResult.Completed);
            }

            _layoutCompletion ??= new();

            int motionId = ++_layoutMotionId;
            StopCurrentLayoutSequence();
            LayoutMotionRun motionRun = new(motionId);
            Sequence sequence = Sequence.Create();

            for (int i = 0; i < visibleItems.Count; i++)
            {
                sequence.Group(Tween.LocalPosition(
                    visibleItems[i].Root,
                    targetPositions[i],
                    _layoutMotionDuration,
                    _layoutMotionEase));
            }

            sequence.ChainCallback(motionRun, target => target.MarkCompleted());
            _layoutSequence = sequence;
            _ = CompleteLayoutMotionAsync(sequence, motionRun);

            return _layoutCompletion.Task;
        }

        public void ResetLayout()
        {
            if (!InitializeLayout())
            {
                return;
            }

            CancelLayoutMotion();

            for (int i = 0; i < _layoutItems.Count; i++)
            {
                PackageLayoutItem item = _layoutItems[i];
                item.Root.localPosition = item.InitialLocalPosition;
            }
        }

        public void CancelLayoutMotion()
        {
            _layoutMotionId++;
            StopCurrentLayoutSequence();

            TaskCompletionSource<MotionResult> completion = _layoutCompletion;
            _layoutCompletion = null;
            completion?.TrySetResult(MotionResult.Cancelled);
        }

        private async Task CompleteLayoutMotionAsync(Sequence sequence, LayoutMotionRun motionRun)
        {
            await sequence;

            if (motionRun.Id != _layoutMotionId)
            {
                return;
            }

            _layoutSequence = default;
            TaskCompletionSource<MotionResult> completion = _layoutCompletion;
            _layoutCompletion = null;
            completion?.TrySetResult(
                motionRun.DidComplete ? MotionResult.Completed : MotionResult.Cancelled);
        }

        private bool InitializeLayout()
        {
            if (_isLayoutInitialized)
            {
                return true;
            }

            _layoutItems.Clear();

            if (_packages != null)
            {
                for (int i = 0; i < _packages.Length; i++)
                {
                    TryAddLayoutItem(_packages[i]);
                }
            }

            LockedRequiredPackageView[] lockedPackages =
                GetComponentsInChildren<LockedRequiredPackageView>(includeInactive: true);

            for (int i = 0; i < lockedPackages.Length; i++)
            {
                TryAddLayoutItem(lockedPackages[i]);
            }

            _layoutItems.Sort((left, right) =>
                left.InitialLocalPosition.x.CompareTo(right.InitialLocalPosition.x));

            if (_layoutItems.Count == 0)
            {
                Debug.LogError("Required package layout items are missing.", this);
                return false;
            }

            float firstPositionX = _layoutItems[0].InitialLocalPosition.x;
            float lastPositionX = _layoutItems[_layoutItems.Count - 1].InitialLocalPosition.x;
            _layoutCenterX = (firstPositionX + lastPositionX) * 0.5f;
            _layoutSpacing = _layoutItems.Count > 1
                ? (lastPositionX - firstPositionX) / (_layoutItems.Count - 1)
                : 0f;
            _isLayoutInitialized = true;

            return true;
        }

        private void TryAddLayoutItem(Component view)
        {
            if (view == null)
            {
                return;
            }

            Transform root = view.transform;

            if (root.parent != transform)
            {
                Debug.LogError($"Required package layout item {root.name} must be a direct child.", view);
                return;
            }

            for (int i = 0; i < _layoutItems.Count; i++)
            {
                if (_layoutItems[i].Root == root)
                {
                    return;
                }
            }

            _layoutItems.Add(new(root));
        }

        private List<PackageLayoutItem> GetVisibleLayoutItems()
        {
            List<PackageLayoutItem> visibleItems = new();

            for (int i = 0; i < _layoutItems.Count; i++)
            {
                PackageLayoutItem item = _layoutItems[i];

                if (item.Root.gameObject.activeSelf)
                {
                    visibleItems.Add(item);
                }
            }

            return visibleItems;
        }

        private Vector3[] GetTargetPositions(IReadOnlyList<PackageLayoutItem> visibleItems)
        {
            Vector3[] targetPositions = new Vector3[visibleItems.Count];
            float firstTargetX = _layoutCenterX - _layoutSpacing * (visibleItems.Count - 1) * 0.5f;

            for (int i = 0; i < visibleItems.Count; i++)
            {
                Vector3 currentPosition = visibleItems[i].Root.localPosition;
                currentPosition.x = firstTargetX + _layoutSpacing * i;
                targetPositions[i] = currentPosition;
            }

            return targetPositions;
        }

        private void ApplyTargetPositions(
            IReadOnlyList<PackageLayoutItem> visibleItems,
            IReadOnlyList<Vector3> targetPositions)
        {
            for (int i = 0; i < visibleItems.Count; i++)
            {
                visibleItems[i].Root.localPosition = targetPositions[i];
            }
        }

        private void CompleteLayoutImmediately()
        {
            _layoutMotionId++;
            StopCurrentLayoutSequence();

            TaskCompletionSource<MotionResult> completion = _layoutCompletion;
            _layoutCompletion = null;
            completion?.TrySetResult(MotionResult.Completed);
        }

        private void StopCurrentLayoutSequence()
        {
            if (_layoutSequence.isAlive)
            {
                _layoutSequence.Stop();
            }

            _layoutSequence = default;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class PackageLayoutItem
        {
            public PackageLayoutItem(Transform root)
            {
                Root = root;
                InitialLocalPosition = root.localPosition;
            }

            public Transform Root { get; }
            public Vector3 InitialLocalPosition { get; }
        }

        private sealed class LayoutMotionRun
        {
            public LayoutMotionRun(int id)
            {
                Id = id;
            }

            public int Id { get; }
            public bool DidComplete { get; private set; }

            public void MarkCompleted()
            {
                DidComplete = true;
            }
        }
    }
}
