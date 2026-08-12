using UnityEngine;

namespace FoodieMatch.UI.Common
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIScreenEdgeMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform _visibleArea;
        [SerializeField] private ScreenEdge _exitEdge;
        [SerializeField, Min(0f)] private float _outsidePadding;
        [SerializeField, Range(0f, 1f)] private float _visibilityProgress = 1f;

        private readonly Vector3[] _targetWorldCorners = new Vector3[4];
        private readonly Vector3[] _visibleAreaWorldCorners = new Vector3[4];

        private RectTransform _target;
        private Vector2 _shownAnchoredPosition;
        private Vector2 _hiddenAnchoredPosition;
        private bool _isInitialized;
        private bool _isApplyingLayout;

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (_visibleArea == null || !isActiveAndEnabled)
            {
                return;
            }

            if (!Initialize())
            {
                return;
            }

            RefreshLayout();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                RefreshLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_isInitialized && !_isApplyingLayout)
            {
                RefreshLayout();
            }
        }

        private void OnDidApplyAnimationProperties()
        {
            if (!Initialize())
            {
                return;
            }

            ApplyVisibilityProgress();
        }

        public void RefreshLayout()
        {
            _isApplyingLayout = true;

            Vector2 currentAnchoredPosition = _target.anchoredPosition;
            _target.anchoredPosition = _shownAnchoredPosition;
            _target.ForceUpdateRectTransforms();

            _target.GetWorldCorners(_targetWorldCorners);
            _visibleArea.GetWorldCorners(_visibleAreaWorldCorners);

            Transform parent = _target.parent;
            Rect targetBounds = GetBoundsInParentSpace(
                parent,
                _targetWorldCorners);
            Rect visibleAreaBounds = GetBoundsInParentSpace(
                parent,
                _visibleAreaWorldCorners);

            _hiddenAnchoredPosition = CalculateHiddenAnchoredPosition(
                targetBounds,
                visibleAreaBounds);

            _target.anchoredPosition = currentAnchoredPosition;
            _isApplyingLayout = false;
            ApplyVisibilityProgress();
        }

        private bool Initialize()
        {
            if (_isInitialized)
            {
                return true;
            }

            if (_visibleArea == null || transform.parent == null)
            {
                return false;
            }

            _target = (RectTransform)transform;
            _shownAnchoredPosition = _target.anchoredPosition;
            _isInitialized = true;
            RefreshLayout();
            return true;
        }

        private Vector2 CalculateHiddenAnchoredPosition(
            Rect targetBounds,
            Rect visibleAreaBounds)
        {
            Vector2 hiddenPosition = _shownAnchoredPosition;

            switch (_exitEdge)
            {
                case ScreenEdge.Top:
                    hiddenPosition.y += Mathf.Max(
                        0f,
                        visibleAreaBounds.yMax -
                        targetBounds.yMin +
                        _outsidePadding);
                    break;

                case ScreenEdge.Bottom:
                    hiddenPosition.y += Mathf.Min(
                        0f,
                        visibleAreaBounds.yMin -
                        targetBounds.yMax -
                        _outsidePadding);
                    break;

                case ScreenEdge.Left:
                    hiddenPosition.x += Mathf.Min(
                        0f,
                        visibleAreaBounds.xMin -
                        targetBounds.xMax -
                        _outsidePadding);
                    break;

                case ScreenEdge.Right:
                    hiddenPosition.x += Mathf.Max(
                        0f,
                        visibleAreaBounds.xMax -
                        targetBounds.xMin +
                        _outsidePadding);
                    break;
            }

            return hiddenPosition;
        }

        private void ApplyVisibilityProgress()
        {
            _isApplyingLayout = true;
            _target.anchoredPosition = Vector2.LerpUnclamped(
                _hiddenAnchoredPosition,
                _shownAnchoredPosition,
                _visibilityProgress);
            _isApplyingLayout = false;
        }

        private static Rect GetBoundsInParentSpace(
            Transform parent,
            Vector3[] worldCorners)
        {
            Vector3 firstCorner = parent.InverseTransformPoint(worldCorners[0]);
            float minX = firstCorner.x;
            float maxX = firstCorner.x;
            float minY = firstCorner.y;
            float maxY = firstCorner.y;

            for (int i = 1; i < worldCorners.Length; i++)
            {
                Vector3 corner = parent.InverseTransformPoint(worldCorners[i]);
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private enum ScreenEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }
    }
}
