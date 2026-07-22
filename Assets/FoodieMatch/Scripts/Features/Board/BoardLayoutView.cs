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
        [SerializeField] private FoodItemView _foodItemPrefab;
        [SerializeField] private Transform _foodItemRoot;

        private readonly Dictionary<FoodItemView, FoodBoardAddress>
            _foodAddresses = new();
        private readonly Dictionary<int, GrillView> _grillViews = new();
        private readonly Dictionary<int, List<FoodItemView>>
            _topTrayFoodItems = new();

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

        public event Action<FoodSelectionContext> FoodSelected;

        public void Construct(
            FoodVisualResolver foodVisualResolver)
        {
            _foodVisualResolver = foodVisualResolver;
        }

        private void Awake()
        {
            if (_foodItemRoot == null)
            {
                Debug.LogWarning("Food item root is missing.", this);
            }
        }

        public void Setup(BoardModel board)
        {
            Clear();

            if (board == null)
            {
                Debug.LogWarning("Board model is missing.", this);
                return;
            }

            if (_grillPrefab == null)
            {
                Debug.LogWarning("Grill prefab is missing.", this);
                return;
            }

            for (int i = 0; i < board.GrillCount; i++)
            {
                GrillModel grillModel = board.GetGrillAt(i);

                if (grillModel == null)
                {
                    continue;
                }

                GrillView grillView = Instantiate(_grillPrefab, transform);
                SetGrillPosition(grillView.transform, grillModel.Position);
                _grillViews.Add(grillModel.PositionIndex, grillView);
                grillView.SetupTrayStack(grillModel.TrayCount);
                SpawnTopTrayFoodItems(grillModel, grillView, useNextTray: false);
                SpawnInitialFoodItems(grillModel, grillView);
            }
        }

        public void Clear()
        {
            ClearFoodItems();
            ClearGrills();
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

            foodView.transform.SetParent(
                null,
                worldPositionStays: true);

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

                SpriteRenderer spriteRenderer =
                    view.GetComponentInChildren<SpriteRenderer>();

                tasks.Add(AnimateSinglePopupHideAsync(
                    view,
                    spriteRenderer));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task AnimateSinglePopupHideAsync(
            FoodItemView view,
            SpriteRenderer spriteRenderer)
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

            if (spriteRenderer != null)
            {
                _ = Tween.Alpha(
                    spriteRenderer,
                    0f,
                    totalDuration);
            }

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

                SpriteRenderer spriteRenderer =
                    view.GetComponentInChildren<SpriteRenderer>();

                tasks.Add(AnimateSinglePopupRevealAsync(
                    view,
                    spriteRenderer));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task AnimateSinglePopupRevealAsync(
            FoodItemView view,
            SpriteRenderer spriteRenderer)
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

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0f;
                spriteRenderer.color = color;

                _ = Tween.Alpha(
                    spriteRenderer,
                    1f,
                    RevealGrowDuration);
            }

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
                        out GrillView grillView) ||
                    grillView == null)
                {
                    continue;
                }

                grillView.SetupTrayStack(grillModel.TrayCount);

                List<FoodItemView> trayViews = SpawnTopTrayFoodItems(
                    grillModel,
                    grillView,
                    useNextTray: false);
                List<FoodItemView> grillViews = SpawnFoodItems(
                    grillModel.PositionIndex,
                    grillModel.ActiveFoodSlotCount,
                    grillModel.GetFoodTokenIdAt,
                    grillView.GetFoodAnchor,
                    FoodItemVisualState.OnGrill,
                    true,
                    $"grill position {grillModel.PositionIndex}");

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
            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> entry in _foodAddresses)
            {
                if (entry.Key != null)
                {
                    entry.Key.Selected -= HandleFoodSelected;
                }
            }

            _foodAddresses.Clear();
            _topTrayFoodItems.Clear();

            if (_foodItemRoot == null)
            {
                return;
            }

            for (int childIndex = _foodItemRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(_foodItemRoot.GetChild(childIndex).gameObject);
            }
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
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillView grillView))
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
        }

        public FoodItemView CreateTransientFoodItemView(
            Sprite sprite,
            Vector3 worldPosition,
            int foodTokenId)
        {
            if (_foodItemPrefab == null ||
                _foodItemRoot == null)
            {
                return null;
            }

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

        public bool TryGetGrillView(int grillPositionIndex, out GrillView grillView)
        {
            return _grillViews.TryGetValue(grillPositionIndex, out grillView) && grillView != null;
        }

        public void RestoreFoodItem(
            FoodItemView foodItemView,
            FoodBoardAddress address)
        {
            if (foodItemView == null ||
                _foodAddresses.ContainsKey(foodItemView))
            {
                return;
            }

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
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillView grillView) ||
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

            List<Vector3> grillTargetPositions =
                new List<Vector3>(topTrayFoodItems.Count);

            for (int i = 0; i < topTrayFoodItems.Count; i++)
            {
                FoodItemView foodItemView = topTrayFoodItems[i];

                if (foodItemView == null)
                {
                    grillTargetPositions.Add(default);
                    continue;
                }

                Transform foodAnchor = grillView.GetFoodAnchor(i);

                if (foodAnchor == null ||
                    foodItemView.FoodTokenId !=
                    grillModel.GetFoodTokenIdAt(i))
                {
                    return false;
                }

                grillTargetPositions.Add(foodAnchor.position);
            }

            _topTrayFoodItems.Remove(grillModel.PositionIndex);
            List<FoodItemView> newTopTrayFoodItems = SpawnTopTrayFoodItems(
                grillModel,
                grillView,
                useNextTray: true);

            moveVisuals = new TopTrayMoveVisuals(
                topTrayFoodItems,
                grillTargetPositions,
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
                    out GrillView grillView))
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

            foodItemView.transform.SetPositionAndRotation(foodAnchor.position, foodAnchor.rotation);
            foodItemView.SetVisualState(FoodItemVisualState.OnGrill);
            foodItemView.SetInteractable(makeInteractable);

            if (!makeInteractable)
            {
                return true;
            }

            FoodBoardAddress address = new FoodBoardAddress(
                grillModel.PositionIndex,
                foodItemIndex);

            _foodAddresses.Add(foodItemView, address);
            foodItemView.Selected += HandleFoodSelected;

            return true;
        }

        public bool CompleteTopTrayTransition(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals)
        {
            return grillModel != null &&
                   moveVisuals.DepartingTray != null &&
                   _grillViews.TryGetValue(grillModel.PositionIndex, out GrillView grillView) &&
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
                false,
                $"top tray on grill position {grillModel.PositionIndex}");

            _topTrayFoodItems[grillModel.PositionIndex] = foodItemViews;
            return foodItemViews;
        }

        private void SpawnInitialFoodItems(
            GrillModel grillModel,
            GrillView grillView)
        {
            SpawnFoodItems(
                grillModel.PositionIndex,
                grillModel.ActiveFoodSlotCount,
                grillModel.GetFoodTokenIdAt,
                grillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                true,
                $"grill position {grillModel.PositionIndex}");
        }

        private Sprite ResolveFoodSprite(int foodTokenId)
        {
            return _foodVisualResolver != null
                ? _foodVisualResolver.ResolveIcon(foodTokenId)
                : null;
        }

        private List<FoodItemView> SpawnFoodItems(
            int grillPositionIndex,
            int foodSlotCount,
            Func<int, int> resolveFoodTokenId,
            Func<int, Transform> resolveAnchor,
            FoodItemVisualState visualState,
            bool isInteractable,
            string context)
        {
            List<FoodItemView> foodItemViews =
                new List<FoodItemView>(foodSlotCount);

            if (_foodItemPrefab == null)
            {
                Debug.LogWarning("Food item prefab is missing.", this);
                return foodItemViews;
            }

            if (_foodItemRoot == null)
            {
                Debug.LogWarning("Food item root is missing.", this);
                return foodItemViews;
            }

            if (resolveFoodTokenId == null)
            {
                return foodItemViews;
            }

            for (int i = 0; i < foodSlotCount; i++)
            {
                int foodTokenId = resolveFoodTokenId(i);

                if (foodTokenId <= 0)
                {
                    foodItemViews.Add(null);
                    continue;
                }

                Transform foodAnchor = resolveAnchor?.Invoke(i);

                if (foodAnchor == null)
                {
                    Debug.LogWarning($"Food anchor {i} is missing for {context}.", this);
                    foodItemViews.Add(null);
                    continue;
                }

                FoodItemView foodItemView = Instantiate(_foodItemPrefab, _foodItemRoot);
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
            foreach (KeyValuePair<FoodItemView, FoodBoardAddress> entry in _foodAddresses)
            {
                if (entry.Key != null)
                {
                    entry.Key.Selected -= HandleFoodSelected;
                }
            }

            _foodAddresses.Clear();
            _topTrayFoodItems.Clear();

            if (_foodItemRoot == null)
            {
                return;
            }

            for (int childIndex = _foodItemRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(_foodItemRoot.GetChild(childIndex).gameObject);
            }
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
            foreach (KeyValuePair<int, GrillView> entry in _grillViews)
            {
                if (entry.Value != null)
                {
                    Destroy(entry.Value.gameObject);
                }
            }

            _grillViews.Clear();
        }

        private static void SetGrillPosition(Transform grillTransform, GrillPosition position)
        {
            grillTransform.localPosition = new Vector3(position.X, position.Y, 0f);
            grillTransform.localRotation = Quaternion.identity;
        }
    }
}
