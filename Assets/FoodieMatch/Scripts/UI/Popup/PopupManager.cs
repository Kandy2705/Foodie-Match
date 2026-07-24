using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.UI.Popup
{
    public sealed class PopupManager : MonoBehaviour
    {
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private List<PopupPrefabEntry> _popupPrefabs = new();

        private readonly Dictionary<Type, PopupPrefabEntry> _entryMap = new();
        private readonly Dictionary<Type, PopupBase> _openedPopups = new();
        private readonly Dictionary<Type, PopupBase> _cachedPopups = new();
        private readonly Dictionary<Type, PopupBase> _scenePopups = new();

        private void Awake()
        {
            BuildEntryMap();
        }

        public void RegisterScenePopup<TPopup>(TPopup popup)
            where TPopup : PopupBase
        {
            if (popup == null)
            {
                Debug.LogError(
                    $"Cannot register scene popup " +
                    $"{typeof(TPopup).Name} because it is null.");

                return;
            }

            if (_popupRoot == null)
            {
                Debug.LogError(
                    "Cannot register scene popup because " +
                    "PopupRoot is missing.");

                return;
            }

            Type popupType = typeof(TPopup);

            if (_scenePopups.TryGetValue(
                    popupType,
                    out PopupBase registeredPopup))
            {
                if (registeredPopup == popup)
                {
                    return;
                }

                Debug.LogError(
                    $"A different scene popup is already " +
                    $"registered for type {popupType.Name}.",
                    popup);

                return;
            }

            _scenePopups.Add(popupType, popup);

            popup.HideRequested -= OnPopupHideRequested;
            popup.HideRequested += OnPopupHideRequested;
        }

        public void UnregisterScenePopup<TPopup>(TPopup popup)
            where TPopup : PopupBase
        {
            Type popupType = typeof(TPopup);

            if (!_scenePopups.TryGetValue(
                    popupType,
                    out PopupBase registeredPopup))
            {
                return;
            }

            if (registeredPopup != popup)
            {
                return;
            }

            popup.HideRequested -= OnPopupHideRequested;

            _openedPopups.Remove(popupType);
            _scenePopups.Remove(popupType);
        }

        public TPopup Show<TPopup>(IPopupData data = null)
            where TPopup : PopupBase
        {
            Type popupType = typeof(TPopup);

            if (_openedPopups.TryGetValue(popupType, out PopupBase openedPopup))
            {
                openedPopup.Setup(data);
                openedPopup.Show();
                return openedPopup as TPopup;
            }

            if (_scenePopups.TryGetValue(popupType, out PopupBase scenePopup))
            {
                if (scenePopup == null)
                {
                    _scenePopups.Remove(popupType);

                    Debug.LogError(
                        $"Registered scene popup " +
                        $"{popupType.Name} was destroyed.");

                    return null;
                }

                scenePopup.transform.SetAsFirstSibling();

                scenePopup.Setup(data);
                scenePopup.Show();
                _openedPopups.Add(popupType, scenePopup);
                return scenePopup as TPopup;
            }

            if (_popupRoot == null)
            {
                Debug.LogError("Cannot show popup because PopupRoot is missing.");
                return null;
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

        public void HideAllRuntimePopups()
        {
            List<Type> openedTypes =
                new(_openedPopups.Keys);

            for (int i = 0;
                 i < openedTypes.Count;
                 i++)
            {
                Type popupType =
                    openedTypes[i];

                if (_scenePopups.ContainsKey(
                        popupType))
                {
                    continue;
                }

                Hide(popupType);
            }
        }

        public bool IsOpened<TPopup>()
            where TPopup : PopupBase
        {
            return _openedPopups.ContainsKey(typeof(TPopup));
        }

        public bool TryGetOpened<TPopup>(out TPopup popup)
            where TPopup : PopupBase
        {
            if (_openedPopups.TryGetValue(typeof(TPopup), out PopupBase openedPopup))
            {
                popup = openedPopup as TPopup;
                return popup != null;
            }

            popup = null;
            return false;
        }

        private void Hide(Type popupType)
        {
            if (!_openedPopups.TryGetValue(popupType, out PopupBase popup))
            {
                return;
            }

            popup.Hide();
            _openedPopups.Remove(popupType);

            if (_scenePopups.ContainsKey(popupType))
            {
                return;
            }

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
                cachedPopup.transform.SetAsLastSibling();
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
            popup.transform.SetAsLastSibling();
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
