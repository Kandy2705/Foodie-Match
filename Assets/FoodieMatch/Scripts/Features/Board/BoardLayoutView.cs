using FoodieMatch.Data.Level;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        [SerializeField] private BoardPositionView[] _positions;
        [SerializeField] private GrillView _grillPrefab;
        [SerializeField] private FoodVisualResolver _foodVisualResolver;

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

                var position = FindPosition(grillData.PositionIndex);

                if (position == null)
                {
                    Debug.LogWarning($"Board position {grillData.PositionIndex} is missing.", this);
                    continue;
                }

                var grillView = Instantiate(_grillPrefab, position.ContentRoot);
                grillView.Setup(grillData.InitialFoodTokenIds, _foodVisualResolver);
            }
        }

        public void Clear()
        {
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

                var contentRoot = _positions[i].ContentRoot;

                for (var childIndex = contentRoot.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Destroy(contentRoot.GetChild(childIndex).gameObject);
                }
            }
        }

        private BoardPositionView FindPosition(int positionIndex)
        {
            if (_positions == null)
            {
                return null;
            }

            for (var i = 0; i < _positions.Length; i++)
            {
                if (_positions[i] != null && _positions[i].PositionIndex == positionIndex)
                {
                    return _positions[i];
                }
            }

            return null;
        }
    }
}
