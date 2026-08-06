using UnityEngine;

namespace FoodieMatch.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaHeaderFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform _headerRoot;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private bool _updateWhenScreenChanges = true;

        private float _baseHeight;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private bool _initialized;

        private void Awake()
        {
            if (_headerRoot == null)
            {
                _headerRoot = transform as RectTransform;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        private void OnEnable()
        {
            Canvas.ForceUpdateCanvases();

            if (!_initialized)
            {
                _baseHeight = _headerRoot.rect.height;
                _initialized = true;
            }

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
            return Screen.width != _lastScreenWidth ||
                   Screen.height != _lastScreenHeight ||
                   Screen.safeArea != _lastSafeArea;
        }

        private void RefreshHeaderHeight()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;

            float topInsetPixels =
                Screen.height - Screen.safeArea.yMax;

            float topInsetCanvasUnits =
                topInsetPixels / _canvas.rootCanvas.scaleFactor;

            float targetHeight =
                _baseHeight + topInsetCanvasUnits;

            _headerRoot.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                targetHeight
            );
        }
    }
}
