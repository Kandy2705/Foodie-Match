using System;
using UnityEngine;
namespace FoodieMatch.Runtime.UI.Popup
{
    public abstract class PopupBase : MonoBehaviour
    {
        public event Action<PopupBase> HideRequested;

        public bool IsOpened { get; private set; }

        public virtual void Setup(IPopupData data)
        {
        }

        public virtual void Show()
        {
            IsOpened = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public virtual void Hide()
        {
            IsOpened = false;
            gameObject.SetActive(false);
        }

        public virtual void SetActions(
            Action primaryAction,
            Action secondaryAction)
        {
        }

        public virtual void Dispose()
        {
            HideRequested = null;
        }

        protected void RequestHide()
        {
            HideRequested?.Invoke(this);
        }
    }
}
