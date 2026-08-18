using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        private enum FoodInteractionMode
        {
            Normal,
            Disabled,
            TargetOnly
        }

        [SerializeField] private Transform _foodItemRoot;
        [SerializeField] private Transform _foodInteractionTopBoundary;

        [Header("Food Selection")]
        [SerializeField]
        private Vector2 _selectionSizeAtReferenceHeight =
            new(120f, 180f);

        private readonly Dictionary<FoodItemView, FoodBoardAddress>
            _foodAddresses = new();
        private readonly Dictionary<int, GrillViewBase> _grillViews = new();
        private readonly Dictionary<int, List<FoodItemView>>
            _topTrayFoodItems = new();
        private readonly Dictionary<int, FoodItemView>
            _revealingSingleGrillFoodItems = new();

        private const float HidePunchScaleMultiplier = 1.1f;
        private const float HidePunchDuration = 0.05f;
        private const float HideShrinkDuration = 0.15f;

        private const float RevealOvershootScaleMultiplier = 1.2f;
        private const float RevealGrowDuration = 0.18f;
        private const float RevealSettleDuration = 0.08f;
        private const float ReferenceScreenHeight = 1920f;

        [Header("Top Tray Release Animation")]
        [SerializeField, Min(0f)]
        private float _topTrayReleaseScaleDuration = 0.18f;

        [Header("Grill Intro Motion")]
        [SerializeField] private float _grillIntroRowStagger = 0.06f;
        [SerializeField] private float _grillIntroMaxStagger = 0.2f;

        private FoodVisualResolver _foodVisualResolver;
        private FoodItemViewPool _foodItemViewPool;
        private GrillViewPool _grillViewPool;
        private TrayViewPool _trayViewPool;
        private Camera _worldCamera;
        private GrillMovementController _grillMovementController;
        private StackedGrillLayoutController _stackedGrillLayoutController;
        private FoodInteractionMode _foodInteractionMode;
        private FoodBoardAddress _foodInteractionTarget;

        public event Action StackedGrillMotionFinished;

        public bool HasActiveStackedGrillMotion =>
            _stackedGrillLayoutController?.HasActiveMotion == true;

        public void Construct(
            FoodVisualResolver foodVisualResolver,
            FoodItemViewPool foodItemViewPool,
            GrillViewPool grillViewPool,
            TrayViewPool trayViewPool,
            Camera worldCamera)
        {
            _foodVisualResolver = foodVisualResolver;
            _foodItemViewPool = foodItemViewPool;
            _grillViewPool = grillViewPool;
            _trayViewPool = trayViewPool;
            _worldCamera = worldCamera;
        }

        private void Update()
        {
            _grillMovementController?.Advance(Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_worldCamera == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector2 selectionSize = GetSelectionSize();

            foreach (FoodItemView foodItemView in _foodAddresses.Keys)
            {
                if (IsFoodSelectable(foodItemView))
                {
                    Gizmos.DrawWireCube(
                        foodItemView.transform.position,
                        selectionSize);
                }
            }
        }
#endif

        public void Setup(
            BoardModel board,
            IReadOnlyList<GrillMovementGroupDefinition>
            movementGroups)
        {
            Clear();

            for (int i = 0; i < board.GrillCount; i++)
            {
                GrillModel grillModel = board.GetGrillAt(i);
                GrillViewBase grillView = _grillViewPool.Get(
                    board.LayoutType,
                    grillModel.Type,
                    transform);
                SetGrillPosition(grillView.transform, grillModel.Position);
                _grillViews.Add(grillModel.PositionIndex, grillView);

                if (!SetupGrillView(
                        board.LayoutType,
                        grillModel,
                        grillView))
                {
                    Debug.LogError($"Grill view {grillModel.PositionIndex} could not be set up.", this);
                    Clear();
                    return;
                }

                SpawnInitialFoodItems(
                    board,
                    grillModel,
                    grillView);
            }

            StartStackedGrillLayout(board);
            PrepareGrillMovement(
                board,
                movementGroups);
        }

        private bool SetupGrillView(
            GrillLayoutType layoutType,
            GrillModel grillModel,
            GrillViewBase grillView)
        {
            if (layoutType == GrillLayoutType.StackedColumns)
            {
                return grillView is StackedGrillView;
            }

            if (grillModel.Type == GrillType.Single)
            {
                if (grillView is not SingleGrillView singleGrillView)
                {
                    return false;
                }

                singleGrillView.SetupCounter(grillModel.TrayCount);
                return true;
            }

            if (grillView is not GrillView standardGrillView)
            {
                return false;
            }

            standardGrillView.SetupTrayStack(
                _trayViewPool,
                grillModel.TrayCount);
            SpawnTopTrayFoodItems(grillModel, standardGrillView, useNextTray: false);
            return true;
        }

        private List<FoodItemView> GetTrackedTopTrayFoodItems(int grillPositionIndex)
        {
            return _topTrayFoodItems.TryGetValue(grillPositionIndex, out List<FoodItemView> foodItems)
                ? foodItems
                : new List<FoodItemView>();
        }

        public void Clear()
        {
            StopMotions();
            ClearFoodItems();
            ClearGrills();
            _foodInteractionMode = FoodInteractionMode.Normal;
        }

        public void StopMotions()
        {
            StopGrillIntroMotion();
            StopGrillMovement();
            StopStackedGrillMotion();
        }

        public void PrepareGrillIntro()
        {
            foreach (GrillViewBase grillView in _grillViews.Values)
            {
                grillView.PrepareIntro();
            }
        }

        public async Task<MotionResult> PlayGrillIntroAsync()
        {
            List<float> rowPositions = GetGrillRowPositions();
            List<Task<MotionResult>> motions = new(_grillViews.Count);

            foreach (GrillViewBase grillView in _grillViews.Values)
            {
                int rowIndex = rowPositions.FindIndex(
                    rowY => Mathf.Approximately(
                        rowY,
                        grillView.transform.localPosition.y));
                float startDelay = Mathf.Min(
                    rowIndex * _grillIntroRowStagger,
                    _grillIntroMaxStagger);
                motions.Add(grillView.PlayIntroAsync(startDelay));
            }

            MotionResult[] results = await Task.WhenAll(motions);

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != MotionResult.Completed)
                {
                    return results[i];
                }
            }

            return MotionResult.Completed;
        }

        public void StopGrillIntroMotion()
        {
            foreach (GrillViewBase grillView in _grillViews.Values)
            {
                grillView.StopIntroMotion();
            }
        }

        public void StopGrillMovement()
        {
            _grillMovementController?.StopMovement();
            _grillMovementController = null;
        }

        public void StartGrillMovement()
        {
            _grillMovementController?.StartMovement();
        }

        public bool TryCollectActiveFoodFromTopTrays(
            int foodTokenId,
            out List<FoodItemView> items,
            out List<int> grillPositionIndices,
            out List<int> traySlotIndices)
        {
            items = new List<FoodItemView>();
            grillPositionIndices = new List<int>();
            traySlotIndices = new List<int>();

            foreach (KeyValuePair<int, List<FoodItemView>> kvp in _topTrayFoodItems)
            {
                int positionIndex = kvp.Key;
                List<FoodItemView> foodViews = kvp.Value;

                for (int i = 0; i < foodViews.Count; i++)
                {
                    FoodItemView foodView = foodViews[i];

                    if (foodView != null && foodView.FoodTokenId == foodTokenId)
                    {
                        items.Add(foodView);
                        grillPositionIndices.Add(positionIndex);
                        traySlotIndices.Add(i);
                    }
                }
            }

            return items.Count > 0;
        }

        public void ReleaseTopTrayFoodItem(
            int grillPositionIndex,
            int traySlotIndex)
        {
            if (!_topTrayFoodItems.TryGetValue(
                    grillPositionIndex,
                    out List<FoodItemView> items) ||
                traySlotIndex < 0 ||
                traySlotIndex >= items.Count)
            {
                return;
            }

            FoodItemView foodView = items[traySlotIndex];

            if (foodView == null)
            {
                return;
            }

            Vector3 targetScale =
                foodView.GetVisualScale(
                    FoodItemVisualState.OnGrill);

            MoveFoodToFlightRoot(foodView);

            foodView.transform.rotation =
                Quaternion.identity;

            foodView.SetInteractable(false);

            items[traySlotIndex] = null;

            _ = foodView.PlayScaleAsync(
                targetScale,
                _topTrayReleaseScaleDuration,
                Ease.OutCubic);
        }

        public void RestrictFoodInteractionTo(FoodBoardAddress targetAddress)
        {
            _foodInteractionMode = FoodInteractionMode.TargetOnly;
            _foodInteractionTarget = targetAddress;
            RefreshRegisteredFoodInteraction();
        }

        public void DisableFoodInteractionForTutorial()
        {
            _foodInteractionMode = FoodInteractionMode.Disabled;
            RefreshRegisteredFoodInteraction();
        }

        public void ClearFoodInteractionRestriction()
        {
            _foodInteractionMode = FoodInteractionMode.Normal;
            RefreshRegisteredFoodInteraction();
        }

        private void RefreshRegisteredFoodInteraction()
        {
            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> entry in _foodAddresses)
            {
                FoodItemView foodItemView = entry.Key;

                if (foodItemView == null ||
                    foodItemView.IsEmpty ||
                    foodItemView.IsFlying)
                {
                    continue;
                }

                SetFoodInteractable(
                    foodItemView,
                    entry.Value,
                    true);
            }
        }

        public bool TryGetFoodSelection(
            Vector2 screenPosition,
            out FoodSelectionContext selection)
        {
            Vector3 clickWorldPosition =
                _worldCamera.ScreenToWorldPoint(screenPosition);
            Vector2 clickPosition = clickWorldPosition;
            Vector2 selectionHalfSize =
                GetSelectionSize() * 0.5f;
            float closestDistanceSquared = float.PositiveInfinity;
            FoodItemView closestFood = null;
            FoodBoardAddress closestAddress = default;

            foreach (
                KeyValuePair<FoodItemView, FoodBoardAddress> entry in
                _foodAddresses)
            {
                FoodItemView foodItemView = entry.Key;

                if (!IsFoodSelectable(foodItemView))
                {
                    continue;
                }

                Vector2 foodPosition = foodItemView.transform.position;
                Vector2 offset = clickPosition - foodPosition;

                if (Mathf.Abs(offset.x) > selectionHalfSize.x ||
                    Mathf.Abs(offset.y) > selectionHalfSize.y)
                {
                    continue;
                }

                float distanceSquared = offset.sqrMagnitude;

                if (distanceSquared >= closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared = distanceSquared;
                closestFood = foodItemView;
                closestAddress = entry.Value;
            }

            if (closestFood == null)
            {
                selection = default;
                return false;
            }

            selection = new FoodSelectionContext(
                closestFood,
                closestAddress);
            return true;
        }

        private Vector2 GetSelectionSize()
        {
            float worldScale =
                _worldCamera.orthographicSize * 2f /
                ReferenceScreenHeight;
            return _selectionSizeAtReferenceHeight * worldScale;
        }

        private bool IsFoodSelectable(FoodItemView foodItemView)
        {
            return foodItemView != null &&
                   foodItemView.IsInteractable &&
                   !foodItemView.IsEmpty &&
                   !foodItemView.IsFlying &&
                   foodItemView.InteractionBottomY <=
                   _foodInteractionTopBoundary.position.y;
        }

        public void RefreshStackedGrillLayout()
        {
            if (_stackedGrillLayoutController == null)
            {
                return;
            }

            _stackedGrillLayoutController.RefreshLayout();
        }

        public bool TryGetFoodAddress(
            FoodItemView foodItemView,
            out FoodBoardAddress address)
        {
            if (foodItemView == null)
            {
                address = default;
                return false;
            }

            return _foodAddresses.TryGetValue(foodItemView, out address);
        }

        public Vector2 GetFoodScreenPosition(FoodBoardAddress address)
        {
            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> entry in _foodAddresses)
            {
                if (entry.Value.Equals(address))
                {
                    return _worldCamera.WorldToScreenPoint(entry.Key.transform.position);
                }
            }

            throw new InvalidOperationException(
                $"Food at grill {address.GrillPositionIndex}, " +
                $"slot {address.FoodSlotIndex} is not displayed on the board.");
        }

        public List<FoodBoardEntry> GetActiveFoodEntries()
        {
            List<FoodBoardEntry> entries = new List<FoodBoardEntry>(_foodAddresses.Count);

            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> kvp in _foodAddresses)
            {
                if (kvp.Key == null || kvp.Key.IsEmpty)
                {
                    continue;
                }

                entries.Add(new FoodBoardEntry(kvp.Key, kvp.Value));
            }

            entries.Sort(CompareFoodBoardEntries);
            return entries;
        }

        public List<FoodItemView> GetAllTopTrayFoodViews()
        {
            List<FoodItemView> views = new List<FoodItemView>();

            foreach (KeyValuePair<int, List<FoodItemView>> kvp in _topTrayFoodItems)
            {
                foreach (FoodItemView view in kvp.Value)
                {
                    if (view != null)
                    {
                        views.Add(view);
                    }
                }
            }

            return views;
        }

        private static int CompareFoodBoardEntries(
            FoodBoardEntry left,
            FoodBoardEntry right)
        {
            int grillCompare = left.Address.GrillPositionIndex
                .CompareTo(right.Address.GrillPositionIndex);

            if (grillCompare != 0)
            {
                return grillCompare;
            }

            return left.Address.FoodSlotIndex
                .CompareTo(right.Address.FoodSlotIndex);
        }

        public async Task AnimateHideFoodAsync(
            List<FoodItemView> foodViews)
        {
            if (foodViews == null || foodViews.Count == 0)
            {
                return;
            }

            List<Task> tasks = new List<Task>(foodViews.Count);

            for (int i = 0; i < foodViews.Count; i++)
            {
                FoodItemView view = foodViews[i];

                if (view == null || view.IsEmpty)
                {
                    continue;
                }

                tasks.Add(AnimateSinglePopupHideAsync(view));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task AnimateSinglePopupHideAsync(FoodItemView view)
        {
            if (view == null)
            {
                return;
            }

            Vector3 targetScale =
                view.GetVisualScale(view.VisualState);

            Vector3 punchScale =
                targetScale * HidePunchScaleMultiplier;

            float totalDuration =
                HidePunchDuration + HideShrinkDuration;

            Task fadeTask = view.PlayFadeOutAsync(totalDuration);

            Task scaleTask = view.PlayScaleAsync(
                punchScale,
                HidePunchDuration,
                Ease.OutQuad,
                Vector3.zero,
                HideShrinkDuration,
                Ease.InBack);

            await Task.WhenAll(scaleTask, fadeTask);
        }

        public async Task AnimateRevealFoodAsync(
            List<FoodItemView> foodViews)
        {
            if (foodViews == null || foodViews.Count == 0)
            {
                return;
            }

            List<Task> tasks = new List<Task>(foodViews.Count);

            for (int i = 0; i < foodViews.Count; i++)
            {
                FoodItemView view = foodViews[i];

                if (view == null || view.IsEmpty)
                {
                    continue;
                }

                tasks.Add(AnimateSinglePopupRevealAsync(view));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task AnimateSinglePopupRevealAsync(FoodItemView view)
        {
            if (view == null)
            {
                return;
            }

            Vector3 targetScale =
                view.GetVisualScale(view.VisualState);

            Vector3 overshootScale =
                targetScale * RevealOvershootScaleMultiplier;

            view.transform.localScale = Vector3.zero;
            Task fadeTask = view.PlayFadeInAsync(RevealGrowDuration);

            Task scaleTask = view.PlayScaleAsync(
                overshootScale,
                RevealGrowDuration,
                Ease.OutCubic,
                targetScale,
                RevealSettleDuration,
                Ease.OutQuad);

            await Task.WhenAll(scaleTask, fadeTask);
        }

        public void UpdateFoodSprite(FoodItemView view, int newTokenId, Sprite sprite)
        {
            if (view == null)
            {
                return;
            }

            view.Setup(newTokenId, sprite);
        }

        public List<FoodItemView> RebuildFoodVisuals(
            BoardModel board,
            bool startHidden)
        {
            List<FoodItemView> allViews = new List<FoodItemView>();

            if (board == null)
            {
                return allViews;
            }

            ReleaseFoodVisualsKeepGrills();

            for (int i = 0; i < board.GrillCount; i++)
            {
                GrillModel grillModel = board.GetGrillAt(i);

                if (grillModel == null ||
                    !_grillViews.TryGetValue(
                        grillModel.PositionIndex,
                        out GrillViewBase grillView) ||
                    grillView == null)
                {
                    continue;
                }

                List<FoodItemView> trayViews = SetupGrillView(
                        board.LayoutType,
                        grillModel,
                        grillView)
                    ? GetTrackedTopTrayFoodItems(grillModel.PositionIndex)
                    : new List<FoodItemView>();
                List<FoodItemView> grillViews = SpawnFoodItems(
                    grillModel.PositionIndex,
                    grillModel.ActiveFoodSlotCount,
                    grillModel.GetFoodTokenIdAt,
                    grillView.GetFoodAnchor,
                    FoodItemVisualState.OnGrill,
                    registerSelection: true,
                    isInteractable: true);

                AppendNonNullViews(allViews, grillViews);
                AppendNonNullViews(allViews, trayViews);
            }

            if (startHidden)
            {
                for (int i = 0; i < allViews.Count; i++)
                {
                    allViews[i].ForceHiddenVisual();
                }
            }

            return allViews;
        }

        private void ReleaseFoodVisualsKeepGrills()
        {
            ReleaseTrackedFoodItems();
        }

        private static void AppendNonNullViews(
            List<FoodItemView> target,
            List<FoodItemView> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    target.Add(source[i]);
                }
            }
        }

        public bool RemoveTopTrayVisual(GrillModel grillModel)
        {
            if (grillModel == null ||
                grillModel.Type != GrillType.Standard ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillViewBase grillViewBase) ||
                grillViewBase is not GrillView grillView)
            {
                return false;
            }

            TrayView departingTray = grillView.GetTopTray();

            if (departingTray == null)
            {
                return false;
            }

            _topTrayFoodItems.Remove(grillModel.PositionIndex);

            if (!grillView.HideTopTray(departingTray))
            {
                return false;
            }

            SpawnTopTrayFoodItems(
                grillModel,
                grillView,
                useNextTray: false);

            return true;
        }

        public void ReleaseFoodItem(FoodItemView foodItemView)
        {
            if (foodItemView == null)
            {
                return;
            }

            foodItemView.SetInteractable(false);
            _foodAddresses.Remove(foodItemView);
            foodItemView.transform.SetParent(
                null,
                worldPositionStays: true);
        }

        public bool TryGetStandardGrillView(int grillPositionIndex, out GrillView grillView)
        {
            grillView = null;

            if (!_grillViews.TryGetValue(grillPositionIndex, out GrillViewBase grillViewBase) ||
                grillViewBase is not GrillView standardGrillView)
            {
                return false;
            }

            grillView = standardGrillView;
            return true;
        }

        public bool TryPrepareSingleGrillReveal(
            GrillModel grillModel,
            out FoodItemView foodItemView)
        {
            foodItemView = null;

            if (grillModel == null ||
                grillModel.Type != GrillType.Single ||
                grillModel.ActiveFoodCount != 1 ||
                _revealingSingleGrillFoodItems.ContainsKey(grillModel.PositionIndex) ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillViewBase grillViewBase) ||
                grillViewBase is not SingleGrillView singleGrillView)
            {
                return false;
            }

            singleGrillView.SetRemainingFoodCount(grillModel.TrayCount);

            List<FoodItemView> foodItems = SpawnFoodItems(
                grillModel.PositionIndex,
                grillModel.ActiveFoodSlotCount,
                grillModel.GetFoodTokenIdAt,
                singleGrillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                registerSelection: false,
                isInteractable: false);

            if (foodItems.Count != 1 || foodItems[0] == null)
            {
                return false;
            }

            foodItemView = foodItems[0];
            _revealingSingleGrillFoodItems.Add(grillModel.PositionIndex, foodItemView);
            return true;
        }

        public bool CompleteSingleGrillReveal(
            GrillModel grillModel,
            FoodItemView foodItemView)
        {
            if (grillModel == null ||
                grillModel.Type != GrillType.Single ||
                foodItemView == null ||
                !_revealingSingleGrillFoodItems.TryGetValue(
                    grillModel.PositionIndex,
                    out FoodItemView revealingFoodItem) ||
                revealingFoodItem != foodItemView ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillViewBase grillViewBase) ||
                grillViewBase is not SingleGrillView singleGrillView)
            {
                return false;
            }

            Transform foodAnchor = singleGrillView.GetFoodAnchor(0);

            if (foodAnchor == null ||
                foodItemView.FoodTokenId != grillModel.GetFoodTokenIdAt(0) ||
                _foodAddresses.ContainsKey(foodItemView))
            {
                return false;
            }

            _revealingSingleGrillFoodItems.Remove(grillModel.PositionIndex);
            AttachFoodToGrill(foodItemView, singleGrillView, foodAnchor);
            foodItemView.SetVisualState(FoodItemVisualState.OnGrill);

            FoodBoardAddress address = new(grillModel.PositionIndex, 0);
            _foodAddresses.Add(foodItemView, address);
            SetFoodInteractable(foodItemView, address, true);
            return true;
        }

        public void RestoreFoodItem(
            FoodItemView foodItemView,
            FoodBoardAddress address)
        {
            if (foodItemView == null ||
                !address.IsValid ||
                _foodAddresses.ContainsKey(foodItemView) ||
                !_grillViews.TryGetValue(
                    address.GrillPositionIndex,
                    out GrillViewBase grillView) ||
                grillView == null)
            {
                return;
            }

            Transform foodAnchor =
                grillView.GetFoodAnchor(
                    address.FoodSlotIndex);

            if (foodAnchor == null)
            {
                return;
            }

            AttachFoodToGrill(
                foodItemView,
                grillView,
                foodAnchor);
            _foodAddresses.Add(foodItemView, address);
            SetFoodInteractable(
                foodItemView,
                address,
                true);
        }

        public bool TryPrepareTopTrayFoodMove(
            GrillModel grillModel,
            out TopTrayMoveVisuals moveVisuals)
        {
            moveVisuals = default;

            if (grillModel == null ||
                grillModel.Type != GrillType.Standard ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillViewBase grillViewBase) ||
                grillViewBase is not GrillView grillView ||
                !_topTrayFoodItems.TryGetValue(
                    grillModel.PositionIndex,
                    out List<FoodItemView> topTrayFoodItems))
            {
                return false;
            }

            TrayView departingTray = grillView.GetTopTray();

            if (departingTray == null)
            {
                return false;
            }

            List<Transform> grillTargetAnchors =
                new(topTrayFoodItems.Count);

            for (int i = 0; i < topTrayFoodItems.Count; i++)
            {
                FoodItemView foodItemView = topTrayFoodItems[i];

                if (foodItemView == null)
                {
                    grillTargetAnchors.Add(null);
                    continue;
                }

                Transform foodAnchor = grillView.GetFoodAnchor(i);

                if (foodAnchor == null ||
                    foodItemView.FoodTokenId !=
                    grillModel.GetFoodTokenIdAt(i))
                {
                    return false;
                }

                grillTargetAnchors.Add(foodAnchor);
            }

            for (int i = 0; i < topTrayFoodItems.Count; i++)
            {
                FoodItemView foodItemView =
                    topTrayFoodItems[i];

                if (foodItemView != null)
                {
                    MoveFoodToFlightRoot(foodItemView);
                }
            }

            _topTrayFoodItems.Remove(grillModel.PositionIndex);
            List<FoodItemView> newTopTrayFoodItems = SpawnTopTrayFoodItems(
                grillModel,
                grillView,
                useNextTray: true);

            moveVisuals = new TopTrayMoveVisuals(
                topTrayFoodItems,
                grillTargetAnchors,
                departingTray,
                newTopTrayFoodItems);
            return true;
        }

        public bool CompleteTopTrayFoodMoveAt(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals,
            int foodItemIndex)
        {
            IReadOnlyList<FoodItemView> foodItemViews = moveVisuals.MovingFoodItems;

            if (grillModel == null ||
                foodItemViews == null ||
                foodItemIndex < 0 ||
                foodItemIndex >= foodItemViews.Count ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillViewBase grillViewBase) ||
                grillViewBase is not GrillView grillView)
            {
                return false;
            }

            FoodItemView foodItemView = foodItemViews[foodItemIndex];
            Transform foodAnchor = grillView.GetFoodAnchor(foodItemIndex);

            if (foodItemView == null ||
                foodAnchor == null ||
                foodItemView.FoodTokenId != grillModel.GetFoodTokenIdAt(foodItemIndex) ||
                _foodAddresses.ContainsKey(foodItemView))
            {
                return false;
            }

            AttachFoodToGrill(
                foodItemView,
                grillView,
                foodAnchor);
            foodItemView.SetVisualState(FoodItemVisualState.OnGrill);

            FoodBoardAddress address = new FoodBoardAddress(
                grillModel.PositionIndex,
                foodItemIndex);

            _foodAddresses.Add(foodItemView, address);

            SetFoodInteractable(foodItemView, address, true);

            return true;
        }

        public bool CompleteTopTrayTransition(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals)
        {
            return grillModel != null &&
                   grillModel.Type == GrillType.Standard &&
                   moveVisuals.DepartingTray != null &&
                   _grillViews.TryGetValue(
                       grillModel.PositionIndex,
                       out GrillViewBase grillViewBase) &&
                   grillViewBase is GrillView grillView &&
                   grillView.HideTopTray(moveVisuals.DepartingTray);
        }

        private List<FoodItemView> SpawnTopTrayFoodItems(
            GrillModel grillModel,
            GrillView grillView,
            bool useNextTray)
        {
            TrayModel topTray = grillModel.TopTray;

            if (topTray == null)
            {
                return new List<FoodItemView>();
            }

            Func<int, Transform> resolveAnchor = useNextTray
                ? grillView.GetNextTrayFoodAnchor
                : grillView.GetTopTrayFoodAnchor;

            List<FoodItemView> foodItemViews = SpawnFoodItems(
                grillModel.PositionIndex,
                topTray.SlotCount,
                topTray.GetFoodTokenIdAt,
                resolveAnchor,
                FoodItemVisualState.OnTray,
                registerSelection: false,
                isInteractable: false);

            _topTrayFoodItems[grillModel.PositionIndex] = foodItemViews;
            return foodItemViews;
        }

        private void SpawnInitialFoodItems(
            BoardModel board,
            GrillModel grillModel,
            GrillViewBase grillView)
        {
            SpawnFoodItems(
                grillModel.PositionIndex,
                grillModel.ActiveFoodSlotCount,
                grillModel.GetFoodTokenIdAt,
                grillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                registerSelection: true,
                isInteractable: true);
        }

        private Sprite ResolveFoodSprite(int foodTokenId)
        {
            return _foodVisualResolver.ResolveIcon(foodTokenId);
        }

        private List<FoodItemView> SpawnFoodItems(
            int grillPositionIndex,
            int foodSlotCount,
            Func<int, int> resolveFoodTokenId,
            Func<int, Transform> resolveAnchor,
            FoodItemVisualState visualState,
            bool registerSelection,
            bool isInteractable)
        {
            List<FoodItemView> foodItemViews =
                new List<FoodItemView>(foodSlotCount);

            for (int i = 0; i < foodSlotCount; i++)
            {
                int foodTokenId = resolveFoodTokenId(i);

                if (foodTokenId <= 0)
                {
                    foodItemViews.Add(null);
                    continue;
                }

                Transform foodAnchor = resolveAnchor.Invoke(i);
                GrillViewBase grillView = _grillViews[grillPositionIndex];

                FoodItemView foodItemView = _foodItemViewPool.Get(
                    grillView.transform,
                    foodAnchor.position,
                    foodAnchor.rotation);
                foodItemView.Setup(foodTokenId, ResolveFoodSprite(foodTokenId));
                foodItemView.SetVisualState(visualState);
                foodItemView.SetInteractable(false);
                foodItemViews.Add(foodItemView);

                if (!registerSelection)
                {
                    continue;
                }

                FoodBoardAddress address = new FoodBoardAddress(
                    grillPositionIndex,
                    i);

                _foodAddresses.Add(foodItemView, address);
                SetFoodInteractable(foodItemView, address, isInteractable);
            }

            return foodItemViews;
        }

        private void ClearFoodItems()
        {
            ReleaseTrackedFoodItems();
        }

        private void ClearGrills()
        {
            foreach (KeyValuePair<int, GrillViewBase> entry in _grillViews)
            {
                if (entry.Value != null)
                {
                    _grillViewPool.Release(entry.Value);
                }
            }

            _grillViews.Clear();
        }

        private void PrepareGrillMovement(
            BoardModel board,
            IReadOnlyList<GrillMovementGroupDefinition>
                movementGroups)
        {
            StopGrillMovement();

            if (movementGroups.Count == 0)
            {
                return;
            }

            try
            {
                _grillMovementController =
                    new GrillMovementController(
                        _worldCamera,
                        board,
                        movementGroups,
                        _grillViews);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                _grillMovementController = null;
            }
        }

        private void StartStackedGrillLayout(BoardModel board)
        {
            StopStackedGrillMotion();

            if (board.LayoutType != GrillLayoutType.StackedColumns)
            {
                return;
            }

            _stackedGrillLayoutController =
                new StackedGrillLayoutController(
                    board,
                    _grillViews);
            _stackedGrillLayoutController.MotionFinished +=
                HandleStackedGrillMotionFinished;
        }

        private void StopStackedGrillMotion()
        {
            if (_stackedGrillLayoutController == null)
            {
                return;
            }

            _stackedGrillLayoutController.MotionFinished -=
                HandleStackedGrillMotionFinished;
            _stackedGrillLayoutController.Stop();
            _stackedGrillLayoutController = null;
        }

        private void HandleStackedGrillMotionFinished()
        {
            StackedGrillMotionFinished?.Invoke();
        }

        private void SetFoodInteractable(
            FoodItemView foodItemView,
            FoodBoardAddress address,
            bool isInteractable)
        {
            foodItemView.SetInteractable(
                isInteractable &&
                IsFoodInteractionAllowed(address));
        }

        private bool IsFoodInteractionAllowed(FoodBoardAddress address)
        {
            switch (_foodInteractionMode)
            {
                case FoodInteractionMode.Normal:
                    return true;
                case FoodInteractionMode.Disabled:
                    return false;
                case FoodInteractionMode.TargetOnly:
                    return _foodInteractionTarget.Equals(address);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveFoodToFlightRoot(
            FoodItemView foodItemView)
        {
            if (foodItemView == null)
            {
                return;
            }

            foodItemView.transform.SetParent(
                _foodItemRoot,
                worldPositionStays: true);
        }

        private List<float> GetGrillRowPositions()
        {
            List<float> rowPositions = new();

            foreach (GrillViewBase grillView in _grillViews.Values)
            {
                float rowY = grillView.transform.localPosition.y;

                if (!rowPositions.Exists(
                        position => Mathf.Approximately(position, rowY)))
                {
                    rowPositions.Add(rowY);
                }
            }

            rowPositions.Sort((first, second) =>
                second.CompareTo(first));
            return rowPositions;
        }

        private static void AttachFoodToGrill(
            FoodItemView foodItemView,
            GrillViewBase grillView,
            Transform foodAnchor)
        {
            foodItemView.transform.SetPositionAndRotation(
                foodAnchor.position,
                foodAnchor.rotation);
            foodItemView.transform.SetParent(
                grillView.transform,
                worldPositionStays: true);
        }

        private void ReleaseTrackedFoodItems()
        {
            HashSet<FoodItemView> foodItemViews = new();

            foreach (
                KeyValuePair<FoodItemView, FoodBoardAddress>
                    entry in _foodAddresses)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                foodItemViews.Add(entry.Key);
            }

            foreach (
                KeyValuePair<int, List<FoodItemView>>
                    entry in _topTrayFoodItems)
            {
                List<FoodItemView> topTrayFoodItems =
                    entry.Value;

                for (int i = 0;
                     i < topTrayFoodItems.Count;
                     i++)
                {
                    FoodItemView foodItemView =
                        topTrayFoodItems[i];

                    if (foodItemView != null)
                    {
                        foodItemViews.Add(foodItemView);
                    }
                }
            }

            foreach (KeyValuePair<int, FoodItemView> entry in _revealingSingleGrillFoodItems)
            {
                if (entry.Value != null)
                {
                    foodItemViews.Add(entry.Value);
                }
            }

            for (int childIndex =
                     _foodItemRoot.childCount - 1;
                 childIndex >= 0;
                 childIndex--)
            {
                FoodItemView foodItemView = _foodItemRoot
                    .GetChild(childIndex)
                    .GetComponent<FoodItemView>();
                foodItemViews.Add(foodItemView);
            }

            foreach (FoodItemView foodItemView in foodItemViews)
            {
                _foodItemViewPool.Release(foodItemView);
            }

            _foodAddresses.Clear();
            _topTrayFoodItems.Clear();
            _revealingSingleGrillFoodItems.Clear();
        }

        private static void SetGrillPosition(Transform grillTransform, GrillPosition position)
        {
            grillTransform.localPosition = new Vector3(position.X, position.Y, 0f);
            grillTransform.localRotation = Quaternion.identity;
        }
    }
}
