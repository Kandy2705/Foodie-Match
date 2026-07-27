using System;
using System.Collections.Generic;
using FoodieMatch.UI.Navigation;
using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI.MainMenu
{
    public sealed class MainMenuView : PopupBase
    {
        [Serializable]
        private sealed class ViewEntry
        {
            [SerializeField] private BottomNavigationTab _tab;
            [SerializeField] private MonoBehaviour _view;

            public BottomNavigationTab Tab => _tab;
            public MonoBehaviour View => _view;
        }

        [Header("Root")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Navigation")]
        [SerializeField] private BottomNavigationBarView _bottomNavigationBarView;

        [Header("Views")]
        [SerializeField] private List<ViewEntry> _views = new();

        private readonly Dictionary<Type, MonoBehaviour> _viewsByType = new();
        private readonly Dictionary<BottomNavigationTab, MonoBehaviour> _viewsByTab = new();
        private bool _isInitialized;

        public bool IsVisible =>
            gameObject.activeInHierarchy &&
            _canvasGroup.alpha > 0f;

        private void Awake()
        {
            EnsureInitialized();
        }

        public override void Show()
        {
            EnsureInitialized();
            base.Show();

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _bottomNavigationBarView.ShowTabImmediately(BottomNavigationTab.Home);
        }

        public override void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            base.Hide();
        }

        public TView GetView<TView>() where TView : MonoBehaviour
        {
            EnsureInitialized();
            if (TryGetView(out TView view)) return view;
            return null;
        }

        public bool TryGetView<TView>(out TView view) where TView : MonoBehaviour
        {
            EnsureInitialized();
            Type requestedType = typeof(TView);

            if (_viewsByType.TryGetValue(requestedType, out MonoBehaviour registeredView))
            {
                view = registeredView as TView;
                return view != null;
            }

            foreach (MonoBehaviour candidate in _viewsByType.Values)
            {
                if (candidate is TView typedView)
                {
                    view = typedView;
                    return true;
                }
            }

            view = null;
            return false;
        }

        public MonoBehaviour GetView(BottomNavigationTab tab)
        {
            EnsureInitialized();
            _viewsByTab.TryGetValue(tab, out MonoBehaviour view);
            return view;
        }

        public TView GetView<TView>(BottomNavigationTab tab) where TView : MonoBehaviour
        {
            return GetView(tab) as TView;
        }

        public override void Dispose()
        {
            ClearRegisteredViews();
            _viewsByType.Clear();
            _viewsByTab.Clear();
            _isInitialized = false;
            base.Dispose();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            BuildViewMaps();
            _isInitialized = true;
        }

        private void BuildViewMaps()
        {
            _viewsByType.Clear();
            _viewsByTab.Clear();

            for (int i = 0; i < _views.Count; i++)
            {
                ViewEntry entry = _views[i];
                Type viewType = entry.View.GetType();
                _viewsByType.Add(viewType, entry.View);
                _viewsByTab.Add(entry.Tab, entry.View);
            }
        }

        private void ClearRegisteredViews()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                MonoBehaviour view = _views[i].View;
                if (view is IMainMenuViewLifecycle lifecycle)
                    lifecycle.Clear();
            }
        }
    }
}
