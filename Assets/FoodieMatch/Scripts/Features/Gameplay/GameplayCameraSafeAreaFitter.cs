using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayCameraSafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _referenceOrthographicSize = 10f;
        [SerializeField] private bool _updateWhenScreenChanges;

        private Vector3 _referenceCameraPosition;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private bool _hasReferenceCameraState;

        private void Reset()
        {
            _worldCamera = GetComponent<Camera>();
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
                RefreshCamera(force: true);
            }
        }

        private void Update()
        {
            if (_updateWhenScreenChanges)
            {
                RefreshCamera(force: false);
            }
        }

        private void OnDisable()
        {
            RestoreReferenceCameraState();
        }

        private bool TryCaptureReferenceCameraState()
        {
            if (_hasReferenceCameraState)
            {
                return true;
            }

            if (_worldCamera == null)
            {
                _worldCamera = GetComponent<Camera>();
            }

            if (_worldCamera == null)
            {
                Debug.LogError("Gameplay safe area camera is missing.", this);
                return false;
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

        private void RefreshCamera(bool force)
        {
            int screenHeight = Screen.height;
            Rect safeArea = Screen.safeArea;

            if (!force && screenHeight == _lastScreenHeight && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastScreenHeight = screenHeight;
            _lastSafeArea = safeArea;

            if (screenHeight <= 0 || !IsValidSafeArea(safeArea, screenHeight))
            {
                Debug.LogError("Vertical screen safe area is invalid.", this);
                RestoreReferenceCameraState();
                return;
            }

            float adjustedOrthographicSize = _referenceOrthographicSize * screenHeight / safeArea.height;
            float worldUnitsPerPixel = adjustedOrthographicSize * 2f / screenHeight;
            float safeAreaCenterOffset = safeArea.center.y - screenHeight * 0.5f;

            Vector3 cameraPosition = _referenceCameraPosition;
            cameraPosition.y -= safeAreaCenterOffset * worldUnitsPerPixel;

            _worldCamera.orthographicSize = adjustedOrthographicSize;
            _worldCamera.transform.position = cameraPosition;
        }

        private void RestoreReferenceCameraState()
        {
            if (!_hasReferenceCameraState || _worldCamera == null)
            {
                return;
            }

            _worldCamera.orthographicSize = _referenceOrthographicSize;
            _worldCamera.transform.position = _referenceCameraPosition;
        }

        private static bool IsValidSafeArea(Rect safeArea, int screenHeight)
        {
            return safeArea.height > 0f &&
                   safeArea.yMin >= 0f &&
                   safeArea.yMax <= screenHeight &&
                   IsValidNumber(safeArea.yMin) &&
                   IsValidNumber(safeArea.yMax) &&
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
