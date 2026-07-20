using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class GrillView : MonoBehaviour
    {
        [SerializeField] private Transform[] _foodAnchors;
        [SerializeField] private TrayStackView _trayStackView;

        [Header("Close Lid Motion")]
        [SerializeField] private Transform _lid;
        [SerializeField] private Vector3 _lidDropOffset = new(0f, 0.35f, 0f);
        [SerializeField] private float _lidDropDuration = 0.12f;
        [SerializeField] private Ease _lidDropEase = Ease.OutCubic;

        private SpriteRenderer[] _lidSpriteRenderers;
        private Color[] _lidVisibleColors;
        private Sequence _lidSequence;
        private Vector3 _initialLidLocalPosition;
        private bool _hasInitialLidVisual;
        private bool _didLidMotionFinish;
        private bool _isLidClosed;

        public int FoodAnchorCount => _foodAnchors != null ? _foodAnchors.Length : 0;

        private void Awake()
        {
            EnsureInitialLidVisual();
            HideLid();

            if (_lid == null)
            {
                Debug.LogWarning("Grill lid is missing.", this);
            }

            if (_lidSpriteRenderers == null || _lidSpriteRenderers.Length == 0)
            {
                Debug.LogWarning("Grill lid sprite renderers are missing.", this);
            }
        }

        private void OnDestroy()
        {
            CancelMotion();
        }

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

        public async Task<MotionResult> PlayCloseLidAsync()
        {
            if (_isLidClosed)
            {
                return MotionResult.Completed;
            }

            EnsureInitialLidVisual();

            if (!_hasInitialLidVisual ||
                _lidSequence.isAlive ||
                !IsValidTime(_lidDropDuration) ||
                !IsValidVector(_lidDropOffset))
            {
                return MotionResult.Failed;
            }

            PrepareLidForDrop();
            _didLidMotionFinish = false;

            try
            {
                Sequence sequence = Sequence.Create(Tween.LocalPosition(
                    _lid,
                    _initialLidLocalPosition,
                    _lidDropDuration,
                    _lidDropEase));

                for (int i = 0; i < _lidSpriteRenderers.Length; i++)
                {
                    sequence = sequence.Group(Tween.Alpha(
                        _lidSpriteRenderers[i],
                        _lidVisibleColors[i].a,
                        _lidDropDuration,
                        _lidDropEase));
                }

                sequence = sequence.ChainCallback(this, target => target.MarkLidMotionFinished());
                _lidSequence = sequence;
                await _lidSequence;

                if (!_didLidMotionFinish)
                {
                    return MotionResult.Cancelled;
                }

                _isLidClosed = true;
                return MotionResult.Completed;
            }
            finally
            {
                _lidSequence = default;
            }
        }

        public void CancelMotion()
        {
            if (_lidSequence.isAlive)
            {
                _lidSequence.Stop();
            }

            _lidSequence = default;
            _didLidMotionFinish = false;
            _isLidClosed = false;
            HideLid();
        }

        private void PrepareLidForDrop()
        {
            ResetLidVisual();
            _lid.localPosition = _initialLidLocalPosition + _lidDropOffset;

            for (int i = 0; i < _lidSpriteRenderers.Length; i++)
            {
                Color color = _lidSpriteRenderers[i].color;
                color.a = 0f;
                _lidSpriteRenderers[i].color = color;
            }

            _lid.gameObject.SetActive(true);
        }

        private void HideLid()
        {
            if (_lid == null)
            {
                return;
            }

            ResetLidVisual();
            _lid.gameObject.SetActive(false);
        }

        private void ResetLidVisual()
        {
            EnsureInitialLidVisual();

            if (!_hasInitialLidVisual)
            {
                return;
            }

            _lid.localPosition = _initialLidLocalPosition;

            for (int i = 0; i < _lidSpriteRenderers.Length; i++)
            {
                _lidSpriteRenderers[i].color = _lidVisibleColors[i];
            }
        }

        private void EnsureInitialLidVisual()
        {
            if (_hasInitialLidVisual || _lid == null)
            {
                return;
            }

            SpriteRenderer[] spriteRenderers =
                _lid.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            if (spriteRenderers.Length == 0)
            {
                return;
            }

            _lidSpriteRenderers = spriteRenderers;
            _lidVisibleColors = new Color[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                _lidVisibleColors[i] = spriteRenderers[i].color;
            }

            _initialLidLocalPosition = _lid.localPosition;
            _hasInitialLidVisual = true;
        }

        private void MarkLidMotionFinished()
        {
            _didLidMotionFinish = true;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsValidVector(Vector3 value)
        {
            return IsValidNumber(value.x) && IsValidNumber(value.y) && IsValidNumber(value.z);
        }

        private static bool IsValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
