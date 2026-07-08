using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class TrayView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform[] _foodAnchors;

        public void SetSortingOrder(int sortingOrder)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = sortingOrder;
            }
        }

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
