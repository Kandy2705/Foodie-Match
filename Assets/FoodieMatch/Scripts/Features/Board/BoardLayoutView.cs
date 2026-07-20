using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        [SerializeField] private Transform[] _positions;
        [SerializeField] private GrillView _grillPrefab;
        [SerializeField] private FoodItemView _foodItemPrefab;
        [SerializeField] private Transform _foodItemRoot;

        private readonly Dictionary<FoodItemView, FoodBoardAddress>
            _foodAddresses = new();
        private readonly Dictionary<int, GrillView> _grillViews = new();
        private readonly Dictionary<int, List<FoodItemView>>
            _topTrayFoodItems = new();

        private FoodVisualResolver _foodVisualResolver;

        public event Action<FoodSelectionContext> FoodSelected;

        public void Construct(
            FoodVisualResolver foodVisualResolver)
        {
            _foodVisualResolver = foodVisualResolver;
        }

        private void Awake()
        {
            if (_positions == null || _positions.Length == 0)
            {
                Debug.LogWarning("Board positions are missing.", this);
            }

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

                Transform position = GetPosition(grillModel.PositionIndex);

                if (position == null)
                {
                    Debug.LogWarning($"Board position {grillModel.PositionIndex} is missing.", this);
                    continue;
                }

                GrillView grillView = Instantiate(_grillPrefab, position);
                _grillViews.Add(grillModel.PositionIndex, grillView);
                grillView.SetupTrayStack(grillModel.TrayCount);
                SpawnTopTrayFoodItems(grillModel, grillView, useNextTray: false);
                SpawnInitialFoodItems(grillModel, grillView);
            }
        }

        public void Clear()
        {
            ClearFoodItems();

            if (_positions == null)
            {
                return;
            }

            for (int i = 0; i < _positions.Length; i++)
            {
                if (_positions[i] == null)
                {
                    continue;
                }

                for (int childIndex = _positions[i].childCount - 1; childIndex >= 0; childIndex--)
                {
                    Destroy(_positions[i].GetChild(childIndex).gameObject);
                }
            }
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
            _grillViews.Clear();

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

        private Transform GetPosition(int positionIndex)
        {
            if (_positions == null || positionIndex < 0 || positionIndex >= _positions.Length)
            {
                return null;
            }

            return _positions[positionIndex];
        }
    }
}
