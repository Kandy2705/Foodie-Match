using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardScrollDragRelay :
        MonoBehaviour,
        IBeginDragHandler
    {
        private Action _beginDrag;

        public void SetBeginDragHandler(
            Action beginDrag)
        {
            _beginDrag = beginDrag;
        }

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            _beginDrag?.Invoke();
        }

        private void OnDestroy()
        {
            _beginDrag = null;
        }
    }
}
