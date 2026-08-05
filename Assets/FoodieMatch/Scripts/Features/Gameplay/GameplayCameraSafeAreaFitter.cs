using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayCameraSafeAreaFitter : MonoBehaviour
    {
        private const float ReferenceAspectRatio = 1080f / 1920f;

        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _referenceOrthographicSize = 10f;
        [SerializeField] private bool _updateWhenScreenChanges;

        private Vector3 _referenceCameraPosition;
        private Transform _topRoot;
        private Vector3 _referenceTopRootPosition;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private bool _hasReferenceCameraState;
        private bool _hasReferenceTopRootPosition;
        private bool _isWaitingForValidScreen;

        public void SetTopRoot(Transform topRoot)
        {
            _topRoot = topRoot;
            _referenceTopRootPosition = topRoot.position;
            _hasReferenceTopRootPosition = true;

            if (TryCaptureReferenceCameraState())
            {
                _isWaitingForValidScreen = !TryRefreshCamera(force: true);
            }
        }

        private void Awake()
        {
            if (!TryCaptureReferenceCameraState())
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (TryCaptureReferenceCameraState())
            {
                _isWaitingForValidScreen = !TryRefreshCamera(force: true);
            }
        }

        private void Update()
        {
            if (_isWaitingForValidScreen || _updateWhenScreenChanges)
            {
                _isWaitingForValidScreen = !TryRefreshCamera(force: false);
            }
        }

        private void OnDisable()
        {
            _isWaitingForValidScreen = false;
            RestoreReferenceCameraState();
        }

        private bool TryCaptureReferenceCameraState()
        {
            if (_hasReferenceCameraState)
            {
                return true;
            }

            if (!_worldCamera.orthographic)
            {
                Debug.LogError("Gameplay safe area camera must be orthographic.", this);
                return false;
            }

            if (!IsValidPositiveNumber(_referenceOrthographicSize))
            {
                Debug.LogError("Reference orthographic size must be greater than zero.", this);
                return false;
            }

            _referenceCameraPosition = _worldCamera.transform.position;
            _hasReferenceCameraState = true;
            return true;
        }

        private bool TryRefreshCamera(bool force)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            Rect safeArea = Screen.safeArea;

            if (screenWidth <= 0 ||
                screenHeight <= 0 ||
                !IsValidSafeArea(safeArea, screenWidth, screenHeight))
            {
                RestoreReferenceCameraState();
                return false;
            }

            if (!force &&
                screenWidth == _lastScreenWidth &&
                screenHeight == _lastScreenHeight &&
                safeArea == _lastSafeArea)
            {
                return true;
            }

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            _lastSafeArea = safeArea;

            float verticalOrthographicSize =
                _referenceOrthographicSize *
                screenHeight /
                safeArea.height;
            float horizontalOrthographicSize =
                _referenceOrthographicSize *
                ReferenceAspectRatio *
                screenHeight /
                safeArea.width;
            float adjustedOrthographicSize = Mathf.Max(
                verticalOrthographicSize,
                horizontalOrthographicSize);
            float worldUnitsPerPixel = adjustedOrthographicSize * 2f / screenHeight;
            Vector2 screenCenter = new(screenWidth * 0.5f, screenHeight * 0.5f);
            Vector2 safeAreaCenterOffset = safeArea.center - screenCenter;

            Vector3 cameraPosition = _referenceCameraPosition;
            cameraPosition.x -= safeAreaCenterOffset.x * worldUnitsPerPixel;
            cameraPosition.y -= safeAreaCenterOffset.y * worldUnitsPerPixel;

            _worldCamera.orthographicSize = adjustedOrthographicSize;
            _worldCamera.transform.position = cameraPosition;
            RefreshTopRootPosition(
                safeArea,
                screenHeight,
                worldUnitsPerPixel,
                cameraPosition.y);
            return true;
        }

        private void RefreshTopRootPosition(
            Rect safeArea,
            int screenHeight,
            float worldUnitsPerPixel,
            float cameraPositionY)
        {
            if (!_hasReferenceTopRootPosition)
            {
                return;
            }

            float referenceTopY =
                _referenceCameraPosition.y +
                _referenceOrthographicSize;
            float topRootInset =
                referenceTopY -
                _referenceTopRootPosition.y;
            float safeAreaTopY =
                cameraPositionY +
                (safeArea.yMax - screenHeight * 0.5f) *
                worldUnitsPerPixel;

            Vector3 topRootPosition = _referenceTopRootPosition;
            topRootPosition.y = safeAreaTopY - topRootInset;
            _topRoot.position = topRootPosition;
        }

        private void RestoreReferenceCameraState()
        {
            if (!_hasReferenceCameraState || _worldCamera == null)
            {
                return;
            }

            _worldCamera.orthographicSize = _referenceOrthographicSize;
            _worldCamera.transform.position = _referenceCameraPosition;

            if (_hasReferenceTopRootPosition && _topRoot != null)
            {
                _topRoot.position = _referenceTopRootPosition;
            }
        }

        private static bool IsValidSafeArea(
            Rect safeArea,
            int screenWidth,
            int screenHeight)
        {
            return safeArea.width > 0f &&
                   safeArea.height > 0f &&
                   safeArea.xMin >= 0f &&
                   safeArea.yMin >= 0f &&
                   safeArea.xMax <= screenWidth &&
                   safeArea.yMax <= screenHeight &&
                   IsValidNumber(safeArea.xMin) &&
                   IsValidNumber(safeArea.yMin) &&
                   IsValidNumber(safeArea.xMax) &&
                   IsValidNumber(safeArea.yMax) &&
                   IsValidNumber(safeArea.width) &&
                   IsValidNumber(safeArea.height);
        }

        private static bool IsValidPositiveNumber(float value)
        {
            return value > 0f && IsValidNumber(value);
        }

        private static bool IsValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
