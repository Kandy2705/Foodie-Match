using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodItemView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _clickCollider;

        public int FoodTokenId { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsInteractable { get; private set; }

        public event Action<FoodItemView> Selected;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_clickCollider == null)
            {
                _clickCollider = GetComponent<Collider2D>();
            }

            ApplyColliderState();
        }

        public void Setup(int foodTokenId, Sprite sprite)
        {
            if (foodTokenId < 0)
            {
                Debug.LogWarning($"Food token id cannot be negative: {foodTokenId}.", this);
                Clear();
                return;
            }

            if (foodTokenId == 0)
            {
                Clear();
                return;
            }

            FoodTokenId = foodTokenId;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.enabled = sprite != null;
            }

            ApplyColliderState();
        }

        public void Clear()
        {
            FoodTokenId = 0;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = null;
                _spriteRenderer.enabled = false;
            }

            ApplyColliderState();
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            ApplyColliderState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsEmpty || !IsInteractable)
            {
                return;
            }

            Selected?.Invoke(this);
        }

        private void ApplyColliderState()
        {
            if (_clickCollider != null)
            {
                _clickCollider.enabled = !IsEmpty && IsInteractable;
            }
        }
    }
}
