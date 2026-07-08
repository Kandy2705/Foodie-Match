using System;
using System.Collections.Generic;
using FoodieMatch.Data.Level;
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
        [SerializeField] private FoodVisualResolver _foodVisualResolver;
        [SerializeField] private LevelDataSO _levelData;

        private readonly List<FoodItemView> _selectableFoodItems = new();

        public event Action<FoodSelectionContext> FoodSelected;

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

            Setup(_levelData);
        }

        public void Setup(LevelDataSO levelData)
        {
            Clear();

            if (levelData == null)
            {
                Debug.LogWarning("Level data is missing.", this);
                return;
            }

            if (_grillPrefab == null)
            {
                Debug.LogWarning("Grill prefab is missing.", this);
                return;
            }

            foreach (GrillData grillData in levelData.Grills)
            {
                if (grillData == null)
                {
                    continue;
                }

                Transform position = GetPosition(grillData.PositionIndex);

                if (position == null)
                {
                    Debug.LogWarning($"Board position {grillData.PositionIndex} is missing.", this);
                    continue;
                }

                GrillView grillView = Instantiate(_grillPrefab, position);
                grillView.SetupTrayStack(GetTrayCount(grillData));
                SpawnTopTrayFoodItems(grillData, grillView);
                SpawnInitialFoodItems(grillData, grillView);
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

        private static int GetTrayCount(GrillData grillData)
        {
            return grillData.Trays != null ? grillData.Trays.Count : 0;
        }

        private void SpawnTopTrayFoodItems(GrillData grillData, GrillView grillView)
        {
            if (grillData.Trays == null || grillData.Trays.Count == 0 || grillData.Trays[0] == null)
            {
                return;
            }

            SpawnFoodItems(
                grillData.Trays[0].FoodTokenIds,
                grillView.GetTopTrayFoodAnchor,
                FoodItemVisualState.OnTray,
                false,
                $"top tray on grill position {grillData.PositionIndex}");
        }

        private void SpawnInitialFoodItems(GrillData grillData, GrillView grillView)
        {
            SpawnFoodItems(
                grillData.InitialFoodTokenIds,
                grillView.GetFoodAnchor,
                FoodItemVisualState.OnGrill,
                true,
                $"grill position {grillData.PositionIndex}");
        }

        private Sprite ResolveFoodSprite(int foodTokenId)
        {
            return _foodVisualResolver != null
                ? _foodVisualResolver.ResolveIcon(foodTokenId)
                : null;
        }

        private void SpawnFoodItems(
            IReadOnlyList<int> foodTokenIds,
            Func<int, Transform> resolveAnchor,
            FoodItemVisualState visualState,
            bool isInteractable,
            string context)
        {
            if (_foodItemPrefab == null)
            {
                Debug.LogWarning("Food item prefab is missing.", this);
                return;
            }

            if (_foodItemRoot == null)
            {
                Debug.LogWarning("Food item root is missing.", this);
                return;
            }

            if (foodTokenIds == null)
            {
                return;
            }

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                int foodTokenId = foodTokenIds[i];

                if (foodTokenId <= 0)
                {
                    continue;
                }

                Transform foodAnchor = resolveAnchor?.Invoke(i);

                if (foodAnchor == null)
                {
                    Debug.LogWarning($"Food anchor {i} is missing for {context}.", this);
                    continue;
                }

                FoodItemView foodItemView = Instantiate(_foodItemPrefab, _foodItemRoot);
                foodItemView.transform.SetPositionAndRotation(foodAnchor.position, foodAnchor.rotation);
                foodItemView.Setup(foodTokenId, ResolveFoodSprite(foodTokenId));
                foodItemView.SetVisualState(visualState);
                foodItemView.SetInteractable(isInteractable);

                if (isInteractable)
                {
                    foodItemView.Selected += HandleFoodSelected;
                    _selectableFoodItems.Add(foodItemView);
                }
            }
        }

        private void ClearFoodItems()
        {
            for (int i = 0; i < _selectableFoodItems.Count; i++)
            {
                if (_selectableFoodItems[i] != null)
                {
                    _selectableFoodItems[i].Selected -= HandleFoodSelected;
                }
            }

            _selectableFoodItems.Clear();

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
            FoodSelected?.Invoke(new FoodSelectionContext(foodItemView));
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
