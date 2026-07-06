using System;
using UnityEngine;

namespace FoodieMatch.UI.Popup
{
    public abstract class PopupBase : MonoBehaviour
    {
        public bool IsOpened { get; private set; }

        public event Action<PopupBase> HideRequested;

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
