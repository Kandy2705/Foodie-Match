using System;
using System.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class LockedRequiredPackageView :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private Transform _pressTarget;
        [SerializeField] private Collider2D _clickCollider;
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] private Vector3 _normalScale = Vector3.one;
        [SerializeField] private Vector3 _pressedScale = new Vector3(1.08f, 1.08f, 1f);
        [SerializeField] private bool _isInteractable = true;

        [Header("Unlock Motion")]
        [SerializeField] private SpriteRenderer[] _fadeRenderers;
        [SerializeField] private Vector3 _unlockRiseOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float _unlockDuration = 0.35f;
        [SerializeField] private Ease _unlockEase = Ease.OutCubic;

        [Header("Unlock Squash Motion")]
        [SerializeField]
        private Vector3 _unlockWideScaleMultiplier =
            new Vector3(1.2f, 0.6f, 1f);

        [SerializeField, Min(0f)]
        private float _unlockWideDuration = 0.10f;

        [SerializeField]
        private Ease _unlockWideEase = Ease.OutCubic;

        [SerializeField]
        private Vector3 _unlockTallScaleMultiplier =
            new Vector3(0.7f, 1.2f, 1f);

        [SerializeField, Min(0f)]
        private float _unlockTallDuration = 0.10f;

        [SerializeField]
        private Ease _unlockTallEase = Ease.InOutSine;

        [SerializeField, Min(0f)]
        private float _unlockRestoreDuration = 0.15f;

        [SerializeField]
        private Ease _unlockRestoreEase = Ease.OutCubic;

        public bool IsInteractable { get; private set; }

        public event Action<LockedRequiredPackageView> UnlockRequested;

        private Vector3 _initialLocalPosition;
        private bool _hasCachedInitialTransform;
        private float[] _initialRendererAlphas;
        private Sequence _unlockSequence;

        private void Awake()
        {
            if (_pressTarget == null)
            {
                _pressTarget = transform;
            }

            if (_clickCollider == null)
            {
                _clickCollider = GetComponent<Collider2D>();
            }

            FindSortingGroup();

            if (_sortingGroup == null)
            {
                Debug.LogWarning("Locked required package sorting group is missing.", this);
            }

            EnsureFadeRenderers();
            CacheInitialTransform();
            CacheInitialAlphas();

            IsInteractable = _isInteractable;
            ApplyScale(_normalScale);
            ApplyColliderState();
        }

        private void OnDestroy()
        {
            if (_unlockSequence.isAlive)
            {
                _unlockSequence.Stop();
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            ApplyScale(_normalScale);
            ApplyColliderState();
        }

        public void SetSortingOrder(int sortingOrder)
        {
            FindSortingGroup();

            if (_sortingGroup == null)
            {
                Debug.LogError(
                    "Locked required package sorting order could not be set because its sorting group is missing.",
                    this);
                return;
            }

            _sortingGroup.sortingOrder = sortingOrder;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            ApplyScale(_pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            ApplyScale(_normalScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            ApplyScale(_normalScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            UnlockRequested?.Invoke(this);
        }

        public async Task PlayUnlockDisappearAsync()
        {
            SetInteractable(false);
            CacheInitialTransform();
            EnsureFadeRenderers();

            if (_unlockSequence.isAlive)
            {
                _unlockSequence.Stop();
            }

            Vector3 targetPosition =
                _initialLocalPosition +
                _unlockRiseOffset;

            Transform scaleTarget =
                _pressTarget != null
                    ? _pressTarget
                    : transform;

            Vector3 wideScale =
                Vector3.Scale(
                    _normalScale,
                    _unlockWideScaleMultiplier);

            Vector3 tallScale =
                Vector3.Scale(
                    _normalScale,
                    _unlockTallScaleMultiplier);

            scaleTarget.localScale =
                _normalScale;

            ApplyFadeAlpha(1f);

            Sequence scaleSequence =
                Sequence.Create()
                    .Chain(
                        Tween.Scale(
                            scaleTarget,
                            wideScale,
                            _unlockWideDuration,
                            _unlockWideEase))
                    .Chain(
                        Sequence.Create(
                                Tween.LocalPosition(
                                    transform,
                                    targetPosition,
                                    _unlockDuration,
                                    _unlockEase))
                            .Group(
                                Tween.Custom(
                                    this,
                                    startValue: 1f,
                                    endValue: 0f,
                                    duration: _unlockDuration,
                                    ease: _unlockEase,
                                    onValueChange:
                                        (target, value) =>
                                            target
                                                .ApplyFadeAlpha(value)))
                            .Group(
                                Sequence.Create()
                                    .Chain(
                                        Tween.Scale(
                                            scaleTarget,
                                            tallScale,
                                            _unlockTallDuration,
                                            _unlockTallEase))
                                    .Chain(
                                        Tween.Scale(
                                            scaleTarget,
                                            _normalScale,
                                            _unlockRestoreDuration,
                                            _unlockRestoreEase))));

            Sequence sequence = scaleSequence;

            _unlockSequence = sequence;

            try
            {
                await sequence;
            }
            finally
            {
                _unlockSequence = default;
                gameObject.SetActive(false);
                ResetForReuse();
            }
        }

        public void ResetForReuse()
        {
            CacheInitialTransform();
            transform.localPosition = _initialLocalPosition;
            ApplyFadeAlpha(1f);
            ApplyScale(_normalScale);
            SetInteractable(true);
        }

        private void ApplyScale(Vector3 scale)
        {
            if (_pressTarget != null)
            {
                _pressTarget.localScale = scale;
            }
        }

        private void ApplyColliderState()
        {
            if (_clickCollider != null)
            {
                _clickCollider.enabled = IsInteractable;
            }
        }

        private void ApplyFadeAlpha(float alpha)
        {
            if (_fadeRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _fadeRenderers.Length; i++)
            {
                SpriteRenderer renderer = _fadeRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Color color = renderer.color;
                float baseAlpha =
                    _initialRendererAlphas != null && i < _initialRendererAlphas.Length
                        ? _initialRendererAlphas[i]
                        : 1f;
                color.a = baseAlpha * alpha;
                renderer.color = color;
            }
        }

        private void FindSortingGroup()
        {
            if (_sortingGroup == null)
            {
                _sortingGroup = GetComponent<SortingGroup>();
            }
        }

        private void EnsureFadeRenderers()
        {
            if (_fadeRenderers != null && _fadeRenderers.Length > 0)
            {
                return;
            }

            _fadeRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        }

        private void CacheInitialTransform()
        {
            if (_hasCachedInitialTransform)
            {
                return;
            }

            _initialLocalPosition = transform.localPosition;
            _hasCachedInitialTransform = true;
        }

        private void CacheInitialAlphas()
        {
            if (_fadeRenderers == null)
            {
                _initialRendererAlphas = Array.Empty<float>();
                return;
            }

            _initialRendererAlphas = new float[_fadeRenderers.Length];

            for (int i = 0; i < _fadeRenderers.Length; i++)
            {
                _initialRendererAlphas[i] =
                    _fadeRenderers[i] != null ? _fadeRenderers[i].color.a : 1f;
            }
        }
    }
}
