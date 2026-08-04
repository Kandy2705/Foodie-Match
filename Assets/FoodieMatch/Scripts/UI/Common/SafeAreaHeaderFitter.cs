using UnityEngine;

namespace FoodieMatch.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaHeaderFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform _headerRoot;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private bool _updateWhenScreenChanges;

        private float _heightWithoutSafeArea;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;

        private void Start()
        {
            _heightWithoutSafeArea = _headerRoot.rect.height;
            RefreshHeaderHeight();
        }

        private void Update()
        {
            if (_updateWhenScreenChanges && HasScreenChanged())
            {
                RefreshHeaderHeight();
            }
        }

        private bool HasScreenChanged()
        {
            return Screen.height != _lastScreenHeight || Screen.safeArea != _lastSafeArea;
        }

        private void RefreshHeaderHeight()
        {
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;

            float topSafeAreaInset = Screen.height - Screen.safeArea.yMax;
            float topSafeAreaInsetInCanvasUnits = topSafeAreaInset / _canvas.rootCanvas.scaleFactor;
            float headerHeight = _heightWithoutSafeArea + topSafeAreaInsetInCanvasUnits;

            _headerRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, headerHeight);
        }
    }
}
