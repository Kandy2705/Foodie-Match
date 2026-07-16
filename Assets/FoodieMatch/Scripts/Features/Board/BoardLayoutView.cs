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
                SpawnTopTrayFoodItems(grillModel, grillView);
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
            out IReadOnlyList<FoodItemView> foodItemViews,
            out IReadOnlyList<Vector3> targetPositions)
        {
            foodItemViews = null;
            targetPositions = null;

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

            grillView.HideTopTray();
            _topTrayFoodItems.Remove(grillModel.PositionIndex);
            SpawnTopTrayFoodItems(grillModel, grillView);

            foodItemViews = topTrayFoodItems;
            targetPositions = grillTargetPositions;
            return true;
        }

        public bool CompleteTopTrayFoodMove(
            GrillModel grillModel,
            IReadOnlyList<FoodItemView> foodItemViews,
            bool makeInteractable)
        {
            if (grillModel == null ||
                foodItemViews == null ||
                !_grillViews.TryGetValue(
                    grillModel.PositionIndex,
                    out GrillView grillView))
            {
                return false;
            }

            for (int i = 0; i < foodItemViews.Count; i++)
            {
                FoodItemView foodItemView = foodItemViews[i];

                if (foodItemView == null)
                {
                    continue;
                }

                Transform foodAnchor = grillView.GetFoodAnchor(i);

                if (foodAnchor == null ||
                    foodItemView.FoodTokenId !=
                    grillModel.GetFoodTokenIdAt(i) ||
                    _foodAddresses.ContainsKey(foodItemView))
                {
                    return false;
                }
            }

            for (int i = 0; i < foodItemViews.Count; i++)
            {
                FoodItemView foodItemView = foodItemViews[i];

                if (foodItemView == null)
                {
                    continue;
                }

                Transform foodAnchor = grillView.GetFoodAnchor(i);

                foodItemView.transform.SetPositionAndRotation(
                    foodAnchor.position,
                    foodAnchor.rotation);

                foodItemView.SetVisualState(FoodItemVisualState.OnGrill);
                _ = foodItemView.PlayLandingFeedbackAsync();
                foodItemView.SetInteractable(makeInteractable);

                if (!makeInteractable)
                {
                    continue;
                }

                FoodBoardAddress address = new FoodBoardAddress(
                    grillModel.PositionIndex,
                    i);

                _foodAddresses.Add(foodItemView, address);
                foodItemView.Selected += HandleFoodSelected;
            }

            return true;
        }

        private void SpawnTopTrayFoodItems(
            GrillModel grillModel,
            GrillView grillView)
        {
            TrayModel topTray = grillModel.TopTray;

            if (topTray == null)
            {
                return;
            }

            List<FoodItemView> foodItemViews = SpawnFoodItems(
                grillModel.PositionIndex,
                topTray.SlotCount,
                topTray.GetFoodTokenIdAt,
                grillView.GetTopTrayFoodAnchor,
                FoodItemVisualState.OnTray,
                false,
                $"top tray on grill position {grillModel.PositionIndex}");

            _topTrayFoodItems[grillModel.PositionIndex] = foodItemViews;
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
