using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class LockedRequiredPackageView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Transform _pressTarget;
        [SerializeField] private Collider2D _clickCollider;
        [SerializeField] private Vector3 _normalScale = Vector3.one;
        [SerializeField] private Vector3 _pressedScale = new Vector3(1.08f, 1.08f, 1f);
        [SerializeField] private bool _isInteractable = true;
        public bool IsInteractable { get; private set; }

        public event Action<LockedRequiredPackageView> UnlockRequested;

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

            IsInteractable = _isInteractable;
            ApplyScale(_normalScale);
            ApplyColliderState();
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            ApplyScale(_normalScale);
            ApplyColliderState();
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
    }
}
