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

        public Transform GetNextTrayFoodAnchor(int index)
        {
            return _trayStackView != null
                ? _trayStackView.GetNextTrayFoodAnchor(index)
                : null;
        }

        public TrayView GetTopTray()
        {
            return _trayStackView != null ? _trayStackView.GetTopTray() : null;
        }

        public bool HideTopTray(TrayView expectedTray)
        {
            if (_trayStackView == null || expectedTray == null)
            {
                return false;
            }

            return _trayStackView.HideTopTray(expectedTray);
        }
    }
}
