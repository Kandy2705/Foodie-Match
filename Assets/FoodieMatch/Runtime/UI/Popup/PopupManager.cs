using System;
using System.Collections.Generic;
using UnityEngine;
namespace FoodieMatch.Runtime.UI.Popup
{
    public sealed class PopupManager : MonoBehaviour
    {
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private List<PopupPrefabEntry> _popupPrefabs = new();

        private readonly Dictionary<Type, PopupPrefabEntry> _entryMap = new();
        private readonly Dictionary<Type, PopupBase> _openedPopups = new();
        private readonly Dictionary<Type, PopupBase> _cachedPopups = new();

        private void Awake()
        {
            BuildEntryMap();
        }

        public TPopup Show<TPopup>(IPopupData data = null)
            where TPopup : PopupBase
        {
            if (_popupRoot == null)
            {
                Debug.LogError("Cannot show popup because PopupRoot is missing.");
                return null;
            }

            Type popupType = typeof(TPopup);

            if (_openedPopups.TryGetValue(popupType, out PopupBase openedPopup))
            {
                openedPopup.Setup(data);
                openedPopup.Show();
                return openedPopup as TPopup;
            }

            TPopup popup = GetOrCreatePopup<TPopup>();

            if (popup == null)
            {
                return null;
            }

            popup.Setup(data);
            popup.Show();

            _openedPopups.Add(popupType, popup);

            return popup;
        }

        public void Hide<TPopup>()
            where TPopup : PopupBase
        {
            Hide(typeof(TPopup));
        }

        public void HideAll()
        {
            List<Type> openedTypes = new(_openedPopups.Keys);

            for (int i = 0; i < openedTypes.Count; i++)
            {
                Hide(openedTypes[i]);
            }

            _openedPopups.Clear();
        }

        public bool IsOpened<TPopup>()
            where TPopup : PopupBase
        {
            return _openedPopups.ContainsKey(typeof(TPopup));
        }

        private void Hide(Type popupType)
        {
            if (!_openedPopups.TryGetValue(popupType, out PopupBase popup))
            {
                return;
            }

            popup.Hide();
            _openedPopups.Remove(popupType);

            if (!_entryMap.TryGetValue(popupType, out PopupPrefabEntry entry))
            {
                DestroyPopup(popup);
                return;
            }

            if (entry.CacheAfterHide)
            {
                _cachedPopups[popupType] = popup;
                return;
            }

            DestroyPopup(popup);
        }

        private TPopup GetOrCreatePopup<TPopup>()
            where TPopup : PopupBase
        {
            Type popupType = typeof(TPopup);

            if (_cachedPopups.TryGetValue(popupType, out PopupBase cachedPopup))
            {
                _cachedPopups.Remove(popupType);
                cachedPopup.transform.SetParent(_popupRoot, false);
                return cachedPopup as TPopup;
            }

            if (!_entryMap.TryGetValue(popupType, out PopupPrefabEntry entry))
            {
                Debug.LogError($"Popup prefab entry not found for type: {popupType.Name}");
                return null;
            }

            if (entry.Prefab == null)
            {
                Debug.LogError($"Popup prefab is null for type: {popupType.Name}");
                return null;
            }

            PopupBase popup = Instantiate(entry.Prefab, _popupRoot);
            popup.gameObject.name = entry.Prefab.gameObject.name;
            popup.HideRequested += OnPopupHideRequested;

            return popup as TPopup;
        }

        private void DestroyPopup(PopupBase popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.HideRequested -= OnPopupHideRequested;
            popup.Dispose();

            Destroy(popup.gameObject);
        }

        private void OnPopupHideRequested(PopupBase popup)
        {
            if (popup == null)
            {
                return;
            }

            Hide(popup.GetType());
        }

        private void BuildEntryMap()
        {
            _entryMap.Clear();

            for (int i = 0; i < _popupPrefabs.Count; i++)
            {
                PopupPrefabEntry entry = _popupPrefabs[i];

                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                Type popupType = entry.Prefab.GetType();

                if (_entryMap.ContainsKey(popupType))
                {
                    Debug.LogError($"Duplicated popup prefab type: {popupType.Name}");
                    continue;
                }

                _entryMap.Add(popupType, entry);
            }
        }
    }
}
