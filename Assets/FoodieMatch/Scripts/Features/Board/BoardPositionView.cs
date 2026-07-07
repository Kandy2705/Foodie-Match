using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class BoardPositionView : MonoBehaviour
    {
        [SerializeField] private int _positionIndex;
        [SerializeField] private Transform _contentRoot;

        public int PositionIndex => _positionIndex;
        public Transform ContentRoot => _contentRoot != null ? _contentRoot : transform;
    }
}
