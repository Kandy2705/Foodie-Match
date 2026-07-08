using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class GrillView : MonoBehaviour
    {
        [SerializeField] private Transform[] _foodAnchors;
        [SerializeField] private TrayStackView _trayStackView;

        public int FoodAnchorCount => _foodAnchors != null ? _foodAnchors.Length : 0;

        public void SetupTrayStack(int trayCount)
        {
            if (_trayStackView == null)
            {
                Debug.LogWarning("Tray stack view is missing.", this);
                return;
            }

            _trayStackView.Setup(trayCount);
        }

        public Transform GetFoodAnchor(int index)
        {
            if (_foodAnchors == null || index < 0 || index >= _foodAnchors.Length)
            {
                return null;
            }

            return _foodAnchors[index];
        }

        public Transform GetTopTrayFoodAnchor(int index)
        {
            if (_trayStackView == null)
            {
                return null;
            }

            return _trayStackView.GetTopTrayFoodAnchor(index);
        }
    }
}
