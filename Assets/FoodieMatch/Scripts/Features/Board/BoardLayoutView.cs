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

            foreach (var grillData in levelData.Grills)
            {
                if (grillData == null)
                {
                    continue;
                }

                var position = GetPosition(grillData.PositionIndex);

                if (position == null)
                {
                    Debug.LogWarning($"Board position {grillData.PositionIndex} is missing.", this);
                    continue;
                }

                var grillView = Instantiate(_grillPrefab, position);
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

            for (var i = 0; i < _positions.Length; i++)
            {
                if (_positions[i] == null)
                {
                    continue;
                }

                for (var childIndex = _positions[i].childCount - 1; childIndex >= 0; childIndex--)
                {
                    Destroy(_positions[i].GetChild(childIndex).gameObject);
                }
            }
        }

        private void SpawnInitialFoodItems(GrillData grillData, GrillView grillView)
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

            if (grillData.InitialFoodTokenIds == null)
            {
                return;
            }

            for (var i = 0; i < grillData.InitialFoodTokenIds.Count; i++)
            {
                var foodTokenId = grillData.InitialFoodTokenIds[i];

                if (foodTokenId <= 0)
                {
                    continue;
                }

                var foodAnchor = grillView.GetFoodAnchor(i);

                if (foodAnchor == null)
                {
                    Debug.LogWarning($"Food anchor {i} is missing on grill position {grillData.PositionIndex}.", this);
                    continue;
                }

                var foodItemView = Instantiate(_foodItemPrefab, _foodItemRoot);
                foodItemView.transform.SetPositionAndRotation(foodAnchor.position, foodAnchor.rotation);
                foodItemView.Setup(foodTokenId, ResolveFoodSprite(foodTokenId));
                foodItemView.SetVisualState(FoodItemVisualState.OnGrill);
                foodItemView.SetInteractable(false);
            }
        }

        private Sprite ResolveFoodSprite(int foodTokenId)
        {
            return _foodVisualResolver != null
                ? _foodVisualResolver.ResolveIcon(foodTokenId)
                : null;
        }

        private void ClearFoodItems()
        {
            if (_foodItemRoot == null)
            {
                return;
            }

            for (var childIndex = _foodItemRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(_foodItemRoot.GetChild(childIndex).gameObject);
            }
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
