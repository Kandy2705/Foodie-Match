using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class TrayStackView : MonoBehaviour
    {
        [SerializeField] private TrayView _trayPrefab;
        [SerializeField] private Transform _trayRoot;
        [SerializeField] private Vector3 _trayOffset;
        [SerializeField] private int _baseSortingOrder;
        [SerializeField] private int _sortingOrderStep = 1;

        private readonly List<TrayView> _trays = new();

        public int VisibleTrayCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _trays.Count; i++)
                {
                    if (_trays[i] != null && _trays[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Setup(int trayCount)
        {
            Clear();

            if (trayCount <= 0)
            {
                return;
            }

            if (_trayPrefab == null)
            {
                Debug.LogWarning("Tray prefab is missing.", this);
                return;
            }

            var root = _trayRoot != null ? _trayRoot : transform;

            for (var i = 0; i < trayCount; i++)
            {
                var tray = Instantiate(_trayPrefab, root);
                tray.transform.localPosition = _trayOffset * i;
                tray.transform.localRotation = Quaternion.identity;
                tray.transform.localScale = Vector3.one;
                tray.SetSortingOrder(_baseSortingOrder + (trayCount - i) * _sortingOrderStep);

                _trays.Add(tray);
            }
        }

        public bool HideTopTray(TrayView expectedTray)
        {
            TrayView topTray = GetVisibleTrayAt(0);

            if (topTray == null || topTray != expectedTray)
            {
                return false;
            }

            topTray.gameObject.SetActive(false);
            return true;
        }

        public void Clear()
        {
            for (var i = _trays.Count - 1; i >= 0; i--)
            {
                if (_trays[i] != null)
                {
                    Destroy(_trays[i].gameObject);
                }
            }

            _trays.Clear();
        }

        public Transform GetTopTrayFoodAnchor(int index)
        {
            TrayView topTray = GetVisibleTrayAt(0);

            if (topTray == null)
            {
                return null;
            }

            return topTray.GetFoodAnchor(index);
        }

        public Transform GetNextTrayFoodAnchor(int index)
        {
            TrayView nextTray = GetVisibleTrayAt(1);
            return nextTray != null ? nextTray.GetFoodAnchor(index) : null;
        }

        public TrayView GetTopTray()
        {
            return GetVisibleTrayAt(0);
        }

        private TrayView GetVisibleTrayAt(int visibleIndex)
        {
            int currentVisibleIndex = 0;

            for (int i = 0; i < _trays.Count; i++)
            {
                TrayView tray = _trays[i];

                if (tray == null || !tray.gameObject.activeSelf)
                {
                    continue;
                }

                if (currentVisibleIndex == visibleIndex)
                {
                    return tray;
                }

                currentVisibleIndex++;
            }

            return null;
        }
    }
}
