using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.UI.Navigation;
using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI.MainMenu
{
    public sealed class MainMenuView : PopupBase
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Navigation")]
        [SerializeField] private BottomNavigationBarView _bottomNavigationBarView;

        [Header("Views")]
        [SerializeField] private Transform _viewContainer;

        private readonly Dictionary<Type, MonoBehaviour> _viewsByType = new();
        private readonly Dictionary<BottomNavigationTab, MonoBehaviour> _viewsByTab = new();
        private Func<BottomNavigationTab, Task<MonoBehaviour>> _viewLoader;

        public bool IsVisible =>
            gameObject.activeInHierarchy &&
            _canvasGroup.alpha > 0f;

        public Transform ViewContainer => _viewContainer;

        private void Awake()
        {
            _bottomNavigationBarView.SetTabLoadHandler(
                EnsureTabLoadedAsync);
            _bottomNavigationBarView.TabSelected += OnTabSelected;
        }

        private void OnDestroy()
        {
            _bottomNavigationBarView.TabSelected -= OnTabSelected;
        }

        public override void Show()
        {
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
            if (TryGetView(out TView view)) return view;
            return null;
        }

        public bool TryGetView<TView>(out TView view) where TView : MonoBehaviour
        {
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
            _viewsByTab.TryGetValue(tab, out MonoBehaviour view);
            return view;
        }

        public TView GetView<TView>(BottomNavigationTab tab) where TView : MonoBehaviour
        {
            return GetView(tab) as TView;
        }

        public void SelectTab(BottomNavigationTab tab)
        {
            _bottomNavigationBarView.SelectTab(tab);
        }

        public void SetViewLoader(
            Func<BottomNavigationTab, Task<MonoBehaviour>> viewLoader)
        {
            _viewLoader = viewLoader;
        }

        public void RegisterView(
            BottomNavigationTab tab,
            MonoBehaviour view)
        {
            if (_viewsByTab.TryGetValue(
                    tab,
                    out MonoBehaviour registeredView))
            {
                if (registeredView == view)
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"Main menu tab {tab} already has a registered view.");
            }

            _viewsByTab.Add(tab, view);
            _viewsByType.Add(view.GetType(), view);
            RectTransform screenRoot = CreateScreenRoot(tab);
            view.transform.SetParent(screenRoot, false);
            StretchView(view);
            _bottomNavigationBarView.RegisterScreen(
                tab,
                screenRoot,
                screenRoot.GetComponent<CanvasGroup>());
        }

        public override void Dispose()
        {
            ClearRegisteredViews();
            _viewsByType.Clear();
            _viewsByTab.Clear();
            base.Dispose();
        }

        private void ClearRegisteredViews()
        {
            foreach (MonoBehaviour view in _viewsByTab.Values)
            {
                if (view is IMainMenuViewLifecycle lifecycle)
                {
                    lifecycle.Clear();
                }
            }
        }

        private async Task EnsureTabLoadedAsync(
            BottomNavigationTab tab)
        {
            if (_viewsByTab.ContainsKey(tab))
            {
                return;
            }

            MonoBehaviour view = await _viewLoader(tab);
            RegisterView(tab, view);
            await Task.Yield();
            Canvas.ForceUpdateCanvases();
        }

        private RectTransform CreateScreenRoot(
            BottomNavigationTab tab)
        {
            GameObject screenObject = new(
                $"{tab}ScreenContainer",
                typeof(RectTransform),
                typeof(CanvasGroup));
            screenObject.layer = gameObject.layer;

            RectTransform screenRoot =
                screenObject.GetComponent<RectTransform>();
            screenRoot.SetParent(_viewContainer, false);
            screenRoot.anchorMin = Vector2.zero;
            screenRoot.anchorMax = Vector2.one;
            screenRoot.anchoredPosition = Vector2.zero;
            screenRoot.sizeDelta = Vector2.zero;
            screenRoot.localScale = Vector3.one;
            return screenRoot;
        }

        private static void StretchView(
            MonoBehaviour view)
        {
            RectTransform viewRect =
                (RectTransform)view.transform;
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.anchoredPosition = Vector2.zero;
            viewRect.sizeDelta = Vector2.zero;
            viewRect.localScale = Vector3.one;
        }

        private void OnTabSelected(BottomNavigationTab tab)
        {
            if (_viewsByTab.TryGetValue(tab, out MonoBehaviour view) &&
                view is IMainMenuTabSelectionHandler selectionHandler)
            {
                selectionHandler.OnTabSelected();
            }
        }
    }
}
