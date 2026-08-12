using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardScrollDragRelay :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private Action _beginDrag;
        private ScrollRect _scrollRect;
        private Vector2 _contentStartPosition;
        private float _dragSpeedMultiplier = 0.5f;
        private bool _isDragging;

        public void Configure(
            ScrollRect scrollRect,
            float dragSpeedMultiplier)
        {
            _scrollRect = scrollRect;
            _dragSpeedMultiplier = Mathf.Clamp01(
                dragSpeedMultiplier);
        }

        public void SetBeginDragHandler(
            Action beginDrag)
        {
            _beginDrag = beginDrag;
        }

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            if (_scrollRect != null &&
                _scrollRect.content != null)
            {
                _contentStartPosition =
                    _scrollRect.content.anchoredPosition;
                _isDragging = true;
            }

            _beginDrag?.Invoke();
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (!_isDragging ||
                _scrollRect == null ||
                _scrollRect.content == null)
            {
                return;
            }

            Vector2 draggedPosition =
                _scrollRect.content.anchoredPosition;
            _scrollRect.content.anchoredPosition =
                Vector2.LerpUnclamped(
                    _contentStartPosition,
                    draggedPosition,
                    _dragSpeedMultiplier);
        }

        public void OnEndDrag(
            PointerEventData eventData)
        {
            _isDragging = false;

            if (_scrollRect != null)
            {
                _scrollRect.velocity *= _dragSpeedMultiplier;
            }
        }

        private void OnDestroy()
        {
            _beginDrag = null;
            _scrollRect = null;
        }
    }
}
