using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Features.Food;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        [SerializeField] private GrillView _grillPrefab;
        [SerializeField] private SingleGrillView _singleGrillPrefab;
        [SerializeField] private FoodItemView _foodItemPrefab;
        [SerializeField] private Transform _foodItemRoot;

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

        [Header("Top Tray Release Animation")]
        [SerializeField, Min(0f)]
        private float _topTrayReleaseScaleDuration = 0.18f;

        private FoodVisualResolver _foodVisualResolver;
        private Camera _worldCamera;
        private GrillMovementController _grillMovementController;

        public event Action<FoodSelectionContext> FoodSelected;

        public void Construct(
            FoodVisualResolver foodVisualResolver,
            Camera worldCamera)
        {
            _foodVisualResolver = foodVisualResolver;
            _worldCamera = worldCamera;
        }

        private void Update()
        {
            _grillMovementController?.Advance(Time.deltaTime);
        }

        public void Setup(
            BoardModel board,
            IReadOnlyList<GrillMovementGroupDefinition>
            movementGroups)
        {
            Clear();

            for (int i = 0; i < board.GrillCount; i++)
            {
                GrillModel grillModel = board.GetGrillAt(i);
                GrillViewBase grillPrefab = GetGrillPrefab(grillModel.Type);
                GrillViewBase grillView = Instantiate(grillPrefab, transform);
                SetGrillPosition(grillView.transform, grillModel.Position);
                _grillViews.Add(grillModel.PositionIndex, grillView);

                if (!SetupGrillView(grillModel, grillView))
                {
                    Debug.LogError($"Grill view {grillModel.PositionIndex} could not be set up.", this);
                    Clear();
                    return;
                }

                SpawnInitialFoodItems(grillModel, grillView);
            }

            StartGrillMovement(
                board,
                movementGroups);
        }

        private GrillViewBase GetGrillPrefab(GrillType type)
        {
            return type == GrillType.Single ? _singleGrillPrefab : _grillPrefab;
        }

        private bool SetupGrillView(
            GrillModel grillModel,
            GrillViewBase grillView)
        {
            if (grillModel.Type == GrillType.Single)
            {
                if (grillView is not SingleGrillView singleGrillView)
                {
                    return false;
                }

                return singleGrillView.TrySetHiddenFoodCount(grillModel.TrayCount);
            }

            if (grillView is not GrillView standardGrillView)
            {
                return false;
            }

            standardGrillView.SetupTrayStack(grillModel.TrayCount);
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
            StopGrillMovement();
            ClearFoodItems();
            ClearGrills();
        }

        public void StopGrillMovement()
        {
            _grillMovementController?.StopMovement();
            _grillMovementController = null;
        }

        public bool TryCollectActiveFoodByTokenId(
            int foodTokenId,
            out List<FoodItemView> items,
            out List<FoodBoardAddress> addresses)
        {
            items = new List<FoodItemView>();
            addresses = new List<FoodBoardAddress>();

            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> kvp in _foodAddresses)
            {
                if (kvp.Key != null && kvp.Key.FoodTokenId == foodTokenId)
                {
                    items.Add(kvp.Key);
                    addresses.Add(kvp.Value);
                }
            }

            return items.Count > 0;
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

            _ = Tween.Scale(
                foodView.transform,
                targetScale,
                _topTrayReleaseScaleDuration,
                Ease.OutCubic);
        }

        public List<FoodItemView> GetAllActiveFoodViews()
        {
            List<FoodItemView> views = new List<FoodItemView>();

            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> kvp in _foodAddresses)
            {
                if (kvp.Key != null)
                {
                    views.Add(kvp.Key);
                }
            }

            return views;
        }

        public void SetRegisteredFoodInteractable(bool isInteractable)
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

                foodItemView.SetInteractable(isInteractable);
            }
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

            Sequence sequence = Sequence.Create()
                .Chain(Tween.Scale(
                    view.transform,
                    punchScale,
                    HidePunchDuration,
                    Ease.OutQuad))
                .Chain(Tween.Scale(
                    view.transform,
                    Vector3.zero,
                    HideShrinkDuration,
                    Ease.InBack));

            await sequence;
            await fadeTask;
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

            Sequence sequence = Sequence.Create()
                .Chain(Tween.Scale(
                    view.transform,
                    overshootScale,
                    RevealGrowDuration,
                    Ease.OutCubic))
                .Chain(Tween.Scale(
                    view.transform,
                    targetScale,
                    RevealSettleDuration,
                    Ease.OutQuad));

            await sequence;
            await fadeTask;
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

            DestroyFoodVisualsKeepGrills();

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

                List<FoodItemView> trayViews = SetupGrillView(grillModel, grillView)
                    ? GetTrackedTopTrayFoodItems(grillModel.PositionIndex)
                    : new List<FoodItemView>();
                List<FoodItemView> grillViews = SpawnFoodItems(
                    grillModel.PositionIndex,
                    grillModel.ActiveFoodSlotCount,
                    grillModel.GetFoodTokenIdAt,
                    grillView.GetFoodAnchor,
                    FoodItemVisualState.OnGrill,
                    true);

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

        private void DestroyFoodVisualsKeepGrills()
        {
            DestroyTrackedFoodItems();
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

            foodItemView.Selected -= HandleFoodSelected;
            foodItemView.SetInteractable(false);
            _foodAddresses.Remove(foodItemView);
            MoveFoodToFlightRoot(foodItemView);
        }

        public FoodItemView CreateTransientFoodItemView(
            Sprite sprite,
            Vector3 worldPosition,
            int foodTokenId)
        {
            FoodItemView foodItemView =
                Instantiate(
                    _foodItemPrefab,
                    _foodItemRoot);

            foodItemView.transform
                .SetPositionAndRotation(
                    worldPosition,
                    Quaternion.identity);

            foodItemView.Setup(foodTokenId, sprite);
            foodItemView.SetInteractable(false);

            return foodItemView;
        }

        public void DestroyTransientFoodItemView(
            FoodItemView foodItemView)
        {
            if (foodItemView != null)
            {
                Destroy(foodItemView.gameObject);
            }
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
                grillViewBase is not SingleGrillView singleGrillView ||
                !singleGrillView.TrySetHiddenFoodCount(grillModel.TrayCount))
            {
                return false;
            }

            List<FoodItemView> foodItems = SpawnFoodItems(
                grillModel.PositionIndex,
                grillModel.ActiveFoodSlotCount,
                grillModel.GetFoodTokenIdAt,
                singleGrillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                false);

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
            FoodItemView foodItemView,
            bool makeInteractable)
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
            foodItemView.Selected -= HandleFoodSelected;
            foodItemView.Selected += HandleFoodSelected;
            foodItemView.SetInteractable(makeInteractable);
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
            foodItemView.Selected += HandleFoodSelected;
            foodItemView.SetInteractable(true);
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
            int foodItemIndex,
            bool makeInteractable)
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

            foodItemView.Selected -= HandleFoodSelected;
            foodItemView.Selected += HandleFoodSelected;

            foodItemView.SetInteractable(makeInteractable);

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
                false);

            _topTrayFoodItems[grillModel.PositionIndex] = foodItemViews;
            return foodItemViews;
        }

        private void SpawnInitialFoodItems(
            GrillModel grillModel,
            GrillViewBase grillView)
        {
            SpawnFoodItems(
                grillModel.PositionIndex,
                grillModel.ActiveFoodSlotCount,
                grillModel.GetFoodTokenIdAt,
                grillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                true);
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

                FoodItemView foodItemView =
                    Instantiate(
                        _foodItemPrefab,
                        grillView.transform);
                foodItemView.transform.SetPositionAndRotation(foodAnchor.position, foodAnchor.rotation);
                foodItemView.Setup(foodTokenId, ResolveFoodSprite(foodTokenId));
                foodItemView.SetVisualState(visualState);
                foodItemView.SetInteractable(isInteractable);
                foodItemViews.Add(foodItemView);

                if (!isInteractable)
                {
                    continue;
                }

                FoodBoardAddress address = new FoodBoardAddress(
                    grillPositionIndex,
                    i);

                _foodAddresses.Add(foodItemView, address);
                foodItemView.Selected += HandleFoodSelected;
            }

            return foodItemViews;
        }

        private void ClearFoodItems()
        {
            DestroyTrackedFoodItems();
        }

        private void HandleFoodSelected(FoodItemView foodItemView)
        {
            if (!_foodAddresses.TryGetValue(
                    foodItemView,
                    out FoodBoardAddress address))
            {
                return;
            }

            FoodSelected?.Invoke(
                new FoodSelectionContext(
                    foodItemView,
                    address));
        }

        private void ClearGrills()
        {
            foreach (KeyValuePair<int, GrillViewBase> entry in _grillViews)
            {
                if (entry.Value != null)
                {
                    Destroy(entry.Value.gameObject);
                }
            }

            _grillViews.Clear();
        }

        private void StartGrillMovement(
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
                _grillMovementController.StartMovement();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                _grillMovementController = null;
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

        private void DestroyTrackedFoodItems()
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

                entry.Key.Selected -= HandleFoodSelected;
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

            foreach (FoodItemView foodItemView in foodItemViews)
            {
                Destroy(foodItemView.gameObject);
            }

            _foodAddresses.Clear();
            _topTrayFoodItems.Clear();
            _revealingSingleGrillFoodItems.Clear();

            for (int childIndex =
                     _foodItemRoot.childCount - 1;
                 childIndex >= 0;
                 childIndex--)
            {
                Destroy(
                    _foodItemRoot
                        .GetChild(childIndex)
                        .gameObject);
            }
        }

        private static void SetGrillPosition(Transform grillTransform, GrillPosition position)
        {
            grillTransform.localPosition = new Vector3(position.X, position.Y, 0f);
            grillTransform.localRotation = Quaternion.identity;
        }
    }
}
