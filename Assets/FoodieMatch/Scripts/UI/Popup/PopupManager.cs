using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.UI.AddressableAssets;
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
        private readonly Dictionary<Type, int> _requestVersions = new();

        private IAddressableUiFactory _addressableUiFactory;
        private bool _isShutdown;

        private void Awake()
        {
            BuildEntryMap();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public void Construct(IAddressableUiFactory addressableUiFactory)
        {
            _addressableUiFactory = addressableUiFactory ??
                throw new ArgumentNullException(nameof(addressableUiFactory));
        }

        public async Task<TPopup> ShowAsync<TPopup>(
            IPopupData data = null,
            CancellationToken cancellationToken = default)
            where TPopup : PopupBase
        {
            EnsureReady();

            Type popupType = typeof(TPopup);
            int requestVersion = NextRequestVersion(popupType);

            if (_openedPopups.TryGetValue(
                    popupType,
                    out PopupBase openedPopup) &&
                openedPopup != null)
            {
                openedPopup.Setup(data);
                openedPopup.Show();
                return (TPopup)openedPopup;
            }

            PopupBase popup = await GetOrCreatePopupAsync(
                popupType,
                cancellationToken);

            if (!IsCurrentRequest(popupType, requestVersion))
            {
                popup.Hide();
                return (TPopup)popup;
            }

            popup.Setup(data);
            popup.Show();
            _cachedPopups.Remove(popupType);
            _openedPopups[popupType] = popup;

            return (TPopup)popup;
        }

        // ShopScreen is not in the current Addressables catalog, so its
        // existing popup path remains serialized until it receives an address.
        public TPopup Show<TPopup>(IPopupData data = null)
            where TPopup : PopupBase
        {
            EnsureReady();

            Type popupType = typeof(TPopup);

            if (UiAddressCatalog.TryGetAddress(popupType, out _))
            {
                throw new InvalidOperationException(
                    $"{popupType.FullName} is Addressable and must be shown asynchronously.");
            }

            NextRequestVersion(popupType);

            if (_openedPopups.TryGetValue(
                    popupType,
                    out PopupBase openedPopup) &&
                openedPopup != null)
            {
                openedPopup.Setup(data);
                openedPopup.Show();
                return (TPopup)openedPopup;
            }

            TPopup popup = GetOrCreateLegacyPopup<TPopup>();
            popup.Setup(data);
            popup.Show();
            _openedPopups[popupType] = popup;
            return popup;
        }

        public void Hide<TPopup>()
            where TPopup : PopupBase
        {
            Hide(typeof(TPopup));
        }

        public void HideAll()
        {
            HashSet<Type> requestedTypes = new(_requestVersions.Keys);

            foreach (Type popupType in _openedPopups.Keys)
            {
                requestedTypes.Add(popupType);
            }

            foreach (Type popupType in requestedTypes)
            {
                Hide(popupType);
            }

            _openedPopups.Clear();
        }

        public bool IsOpened<TPopup>()
            where TPopup : PopupBase
        {
            return _openedPopups.TryGetValue(
                typeof(TPopup),
                out PopupBase popup) &&
                popup != null;
        }

        public bool TryGetOpened<TPopup>(out TPopup popup)
            where TPopup : PopupBase
        {
            if (_openedPopups.TryGetValue(
                    typeof(TPopup),
                    out PopupBase openedPopup) &&
                openedPopup != null)
            {
                popup = (TPopup)openedPopup;
                return true;
            }

            popup = null;
            return false;
        }

        public void Shutdown()
        {
            if (_isShutdown)
            {
                return;
            }

            _isShutdown = true;

            HashSet<Type> popupTypes = new(_openedPopups.Keys);

            foreach (Type popupType in _cachedPopups.Keys)
            {
                popupTypes.Add(popupType);
            }

            Type[] requestedTypes = new Type[_requestVersions.Count];
            _requestVersions.Keys.CopyTo(requestedTypes, 0);

            for (int i = 0; i < requestedTypes.Length; i++)
            {
                NextRequestVersion(requestedTypes[i]);
            }

            foreach (Type popupType in popupTypes)
            {
                PopupBase popup = null;

                if (!_openedPopups.TryGetValue(popupType, out popup))
                {
                    _cachedPopups.TryGetValue(popupType, out popup);
                }

                if (popup != null)
                {
                    ReleasePopup(popupType, popup);
                }
                else if (UiAddressCatalog.TryGetAddress(
                             popupType,
                             out string address))
                {
                    _addressableUiFactory?.Release(address);
                }
            }

            _openedPopups.Clear();
            _cachedPopups.Clear();
            _requestVersions.Clear();
        }

        private void Hide(Type popupType)
        {
            NextRequestVersion(popupType);

            if (!_openedPopups.TryGetValue(
                    popupType,
                    out PopupBase popup) ||
                popup == null)
            {
                if (!ShouldCacheAfterHide(popupType) &&
                    UiAddressCatalog.TryGetAddress(
                        popupType,
                        out string pendingAddress))
                {
                    _addressableUiFactory.Release(pendingAddress);
                }

                _openedPopups.Remove(popupType);
                return;
            }

            popup.Hide();
            _openedPopups.Remove(popupType);

            if (ShouldCacheAfterHide(popupType))
            {
                _cachedPopups[popupType] = popup;
                return;
            }

            ReleasePopup(popupType, popup);
        }

        private async Task<PopupBase> GetOrCreatePopupAsync(
            Type popupType,
            CancellationToken cancellationToken)
        {
            if (_cachedPopups.TryGetValue(
                    popupType,
                    out PopupBase cachedPopup) &&
                cachedPopup != null)
            {
                _cachedPopups.Remove(popupType);
                PreparePopupForShow(cachedPopup);
                return cachedPopup;
            }

            _cachedPopups.Remove(popupType);

            if (!UiAddressCatalog.TryGetAddress(
                    popupType,
                    out string address))
            {
                throw new InvalidOperationException(
                    $"No Addressables address is registered for {popupType.FullName}.");
            }

            PopupBase popup;

            try
            {
                popup = await _addressableUiFactory.GetOrCreateAsync<PopupBase>(
                    address,
                    _popupRoot,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Addressables UI] Failed: {address} | " +
                    $"Type: {popupType.FullName} | Parent: {_popupRoot.name} | " +
                    $"Exception: {exception}");
                throw;
            }

            if (!popupType.IsInstanceOfType(popup))
            {
                _addressableUiFactory.Release(address);
                throw new InvalidOperationException(
                    $"Address {address} contains {popup.GetType().FullName}, " +
                    $"not {popupType.FullName}.");
            }

            PreparePopupForShow(popup);
            return popup;
        }

        private TPopup GetOrCreateLegacyPopup<TPopup>()
            where TPopup : PopupBase
        {
            Type popupType = typeof(TPopup);

            if (_cachedPopups.TryGetValue(
                    popupType,
                    out PopupBase cachedPopup) &&
                cachedPopup != null)
            {
                _cachedPopups.Remove(popupType);
                PreparePopupForShow(cachedPopup);
                return (TPopup)cachedPopup;
            }

            _cachedPopups.Remove(popupType);
            PopupPrefabEntry entry = GetEntry(popupType);
            PopupBase popup = Instantiate(entry.Prefab, _popupRoot);
            popup.gameObject.name = entry.Prefab.gameObject.name;
            PreparePopupForShow(popup);
            return (TPopup)popup;
        }

        private void PreparePopupForShow(PopupBase popup)
        {
            popup.transform.SetParent(_popupRoot, false);
            popup.transform.SetAsLastSibling();
            popup.HideRequested -= OnPopupHideRequested;
            popup.HideRequested += OnPopupHideRequested;
        }

        private void ReleasePopup(Type popupType, PopupBase popup)
        {
            popup.HideRequested -= OnPopupHideRequested;
            popup.Dispose();

            if (UiAddressCatalog.TryGetAddress(
                    popupType,
                    out string address))
            {
                _addressableUiFactory?.Release(address);
                return;
            }

            Destroy(popup.gameObject);
        }

        private void OnPopupHideRequested(PopupBase popup)
        {
            Hide(popup.GetType());
        }

        private void BuildEntryMap()
        {
            _entryMap.Clear();

            for (int i = 0; i < _popupPrefabs.Count; i++)
            {
                PopupPrefabEntry entry = _popupPrefabs[i];

                if (entry?.Prefab == null)
                {
                    continue;
                }

                Type popupType = entry.Prefab.GetType();
                _entryMap.Add(popupType, entry);
            }
        }

        private PopupPrefabEntry GetEntry(Type popupType)
        {
            if (TryGetEntry(popupType, out PopupPrefabEntry entry))
            {
                return entry;
            }

            throw new KeyNotFoundException(
                $"Popup {popupType.FullName} is not registered in PopupManager.");
        }

        private bool TryGetEntry(
            Type popupType,
            out PopupPrefabEntry entry)
        {
            return _entryMap.TryGetValue(popupType, out entry);
        }

        private bool ShouldCacheAfterHide(Type popupType)
        {
            if (UiAddressCatalog.TryGetAddress(popupType, out _))
            {
                return true;
            }

            return GetEntry(popupType).CacheAfterHide;
        }

        private int NextRequestVersion(Type popupType)
        {
            _requestVersions.TryGetValue(popupType, out int version);
            version++;
            _requestVersions[popupType] = version;
            return version;
        }

        private bool IsCurrentRequest(Type popupType, int requestVersion)
        {
            return !_isShutdown &&
                _requestVersions.TryGetValue(
                    popupType,
                    out int currentVersion) &&
                currentVersion == requestVersion;
        }

        private void EnsureReady()
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(nameof(PopupManager));
            }

            if (_addressableUiFactory == null)
            {
                throw new InvalidOperationException(
                    "PopupManager must be constructed before showing UI.");
            }
        }
    }
}
