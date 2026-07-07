using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class GrillView : MonoBehaviour
    {
        [SerializeField] private Transform[] _foodAnchors;
        [SerializeField] private Transform _plateStackAnchor;

        public int FoodAnchorCount => _foodAnchors != null ? _foodAnchors.Length : 0;
        public Transform PlateStackAnchor => _plateStackAnchor;

        public Transform GetFoodAnchor(int index)
        {
            if (_foodAnchors == null || index < 0 || index >= _foodAnchors.Length)
            {
                return null;
            }

            return _foodAnchors[index];
        }
    }
}
