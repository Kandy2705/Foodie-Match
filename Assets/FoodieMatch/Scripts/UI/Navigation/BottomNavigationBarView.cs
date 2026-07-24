using System;
using System.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodieMatch.UI.Navigation
{
    public sealed class BottomNavigationBarView :
        MonoBehaviour
    {
        private static readonly int IsSelectedHash =
            Animator.StringToHash("IsSelected");

        [Serializable]
        private sealed class TabBinding
        {
            [SerializeField]
            private BottomNavigationTab _tab;

            [SerializeField]
            private Button _button;

            [SerializeField]
            private RectTransform _target;

            [SerializeField]
            private Image _normalIcon;

            [SerializeField]
            private Sprite _selectedIcon;

            [SerializeField]
            private string _label;

            [SerializeField]
            private RectTransform _screenRoot;

            [SerializeField]
            private CanvasGroup _screenCanvasGroup;

            [NonSerialized]
            public UnityAction ClickAction;

            public BottomNavigationTab Tab => _tab;
            public Button Button => _button;
            public RectTransform Target => _target;
            public Image NormalIcon => _normalIcon;
            public Sprite SelectedIcon => _selectedIcon;
            public string Label => _label;
            public CanvasGroup Screen =>
                _screenCanvasGroup;

            public RectTransform ScreenRect =>
                _screenRoot;

            [NonSerialized]
            public Vector2 InitialAnchoredPosition;
        }

        [Header("Selection Indicator")]
        [SerializeField]
        private RectTransform _selectionIndicator;

        [SerializeField]
        private Animator _selectionAnimator;

        [SerializeField]
        private Image _selectionIcon;

        [SerializeField]
        private TMP_Text _selectionLabel;

        [Header("Tabs")]
        [SerializeField]
        private TabBinding[] _tabs;

        [SerializeField]
        private BottomNavigationTab _initialTab =
            BottomNavigationTab.Home;

        [Header("Screen Slide")]
        [SerializeField, Min(0f)]
        private float _screenSlideDuration = 0.32f;

        [SerializeField]
        private Ease _screenSlideEase =
            Ease.OutCubic;

        private TabBinding _currentTab;
        private bool _isInitialized;
        private bool _isTransitioning;

        private void Start()
        {
            _ = InitializeNavigationSafelyAsync();
        }

        private async Task InitializeNavigationSafelyAsync()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding binding = _tabs[i];

                if (binding?.ScreenRect == null)
                {
                    continue;
                }

                binding.ScreenRect.gameObject.SetActive(true);
            }

            await Task.Yield();

            Canvas.ForceUpdateCanvases();

            InitializeNavigation();

            _isInitialized = true;
        }

        private void OnEnable()
        {
            SubscribeButtons();
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
        }

        private void SubscribeButtons()
        {
            if (_tabs == null)
            {
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding binding = _tabs[i];

                if (binding == null ||
                    binding.Button == null)
                {
                    continue;
                }

                binding.ClickAction =
                    () => HandleTabClicked(binding);

                binding.Button.onClick.AddListener(
                    binding.ClickAction);
            }
        }

        private void UnsubscribeButtons()
        {
            if (_tabs == null)
            {
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding binding = _tabs[i];

                if (binding == null ||
                    binding.Button == null ||
                    binding.ClickAction == null)
                {
                    continue;
                }

                binding.Button.onClick.RemoveListener(
                    binding.ClickAction);

                binding.ClickAction = null;
            }
        }

        private void InitializeNavigation()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            TabBinding initialBinding =
                FindBinding(_initialTab);

            if (initialBinding == null)
            {
                Debug.LogError(
                    $"Initial bottom navigation tab " +
                    $"{_initialTab} is missing.",
                    this);

                return;
            }

            _currentTab = initialBinding;

            BringScreenToFront(initialBinding);

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding b = _tabs[i];

                if (b?.ScreenRect != null)
                {
                    b.InitialAnchoredPosition =
                        b.ScreenRect.anchoredPosition;
                }
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding binding = _tabs[i];

                bool selected =
                    ReferenceEquals(
                        binding,
                        initialBinding);

                SetScreenImmediately(
                    binding,
                    selected);

                SetNormalIconVisible(
                    binding,
                    !selected);
            }

            UpdateIndicatorContent(
                initialBinding);

            MoveIndicatorImmediately(
                initialBinding);

            _selectionAnimator.SetBool(
                IsSelectedHash,
                true);
        }

        private void HandleTabClicked(
            TabBinding selectedBinding)
        {
            if (!_isInitialized ||
                selectedBinding == null ||
                ReferenceEquals(
                    selectedBinding,
                    _currentTab) ||
                _isTransitioning)
            {
                return;
            }

            _ = SelectTabSafelyAsync(
                selectedBinding);
        }

        private async Task SelectTabSafelyAsync(
            TabBinding selectedBinding)
        {
            try
            {
                await SelectTabAsync(
                    selectedBinding);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    this);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async Task SelectTabAsync(
            TabBinding selectedBinding)
        {
            _isTransitioning = true;

            TabBinding previousBinding =
                _currentTab;

            SetNormalIconVisible(
                previousBinding,
                true);

            _selectionAnimator.SetBool(
                IsSelectedHash,
                false);

            _selectionAnimator.Update(0f);

            MoveIndicatorImmediately(
                selectedBinding);

            UpdateIndicatorContent(
                selectedBinding);

            SetNormalIconVisible(
                selectedBinding,
                false);

            _selectionAnimator.SetBool(
                IsSelectedHash,
                true);

            BringScreenToFront(
                selectedBinding);

            await SwitchScreenAsync(
                previousBinding,
                selectedBinding);

            _currentTab =
                selectedBinding;
        }

        private void MoveIndicatorImmediately(
            TabBinding binding)
        {
            if (binding?.Target == null)
            {
                return;
            }

            Vector2 anchoredPosition =
                _selectionIndicator
                    .anchoredPosition;

            anchoredPosition.x =
                GetIndicatorTargetX(
                    binding.Target);

            _selectionIndicator
                .anchoredPosition =
                    anchoredPosition;
        }

        private float GetIndicatorTargetX(
            RectTransform target)
        {
            Vector3 targetWorldCenter =
                target.TransformPoint(
                    target.rect.center);

            Transform indicatorParent =
                _selectionIndicator.parent;

            Vector3 targetLocalPosition =
                indicatorParent
                    .InverseTransformPoint(
                        targetWorldCenter);

            return targetLocalPosition.x;
        }

        private int GetTabIndex(
            TabBinding binding)
        {
            if (_tabs == null ||
                binding == null)
            {
                return -1;
            }

            for (int i = 0;
                 i < _tabs.Length;
                 i++)
            {
                if (ReferenceEquals(
                        _tabs[i],
                        binding))
                {
                    return i;
                }
            }

            return -1;
        }

        private async Task SwitchScreenAsync(
            TabBinding previousBinding,
            TabBinding selectedBinding)
        {
            if (previousBinding == null ||
                selectedBinding == null ||
                ReferenceEquals(
                    previousBinding,
                    selectedBinding))
            {
                return;
            }

            CanvasGroup previousScreen =
                previousBinding.Screen;

            CanvasGroup selectedScreen =
                selectedBinding.Screen;

            RectTransform previousRect =
                previousBinding.ScreenRect;

            RectTransform selectedRect =
                selectedBinding.ScreenRect;

            if (previousScreen == null ||
                selectedScreen == null ||
                previousRect == null ||
                selectedRect == null)
            {
                Debug.LogError(
                    "Bottom navigation screen references " +
                    "are missing.",
                    this);

                return;
            }

            RectTransform screenParent =
                previousRect.parent as RectTransform;

            if (screenParent == null)
            {
                Debug.LogError(
                    "Bottom navigation ScreenRoot must " +
                    "use RectTransform.",
                    this);

                return;
            }

            float screenWidth =
                screenParent.rect.width;

            if (screenWidth <= 0f)
            {
                return;
            }

            int previousIndex =
                GetTabIndex(previousBinding);

            int selectedIndex =
                GetTabIndex(selectedBinding);

            bool selectedIsOnLeft =
                selectedIndex < previousIndex;

            Vector2 previousBasePosition =
                previousBinding.InitialAnchoredPosition;

            Vector2 selectedBasePosition =
                selectedBinding.InitialAnchoredPosition;

            float previousTargetX =
                previousBasePosition.x +
                (selectedIsOnLeft
                    ? screenWidth
                    : -screenWidth);

            float selectedStartX =
                selectedBasePosition.x +
                (selectedIsOnLeft
                    ? -screenWidth
                    : screenWidth);

            previousScreen.interactable = false;
            previousScreen.blocksRaycasts = false;

            selectedScreen.interactable = false;
            selectedScreen.blocksRaycasts = false;
            selectedScreen.alpha = 0f;

            previousRect.anchoredPosition =
                previousBasePosition;

            selectedRect.anchoredPosition =
                new Vector2(
                    selectedStartX,
                    selectedBasePosition.y);

            await Task.Yield();

            selectedScreen.alpha = 1f;

            Sequence sequence =
                Sequence.Create(
                    Tween.UIAnchoredPositionX(
                        previousRect,
                        previousTargetX,
                        _screenSlideDuration,
                        _screenSlideEase))
                .Group(
                    Tween.UIAnchoredPositionX(
                        selectedRect,
                        selectedBasePosition.x,
                        _screenSlideDuration,
                        _screenSlideEase));

            await sequence;

            previousScreen.alpha = 0f;
            previousScreen.interactable = false;
            previousScreen.blocksRaycasts = false;

            previousRect.anchoredPosition =
                previousBasePosition;

            selectedRect.anchoredPosition =
                selectedBasePosition;

            selectedScreen.alpha = 1f;
            selectedScreen.interactable = true;
            selectedScreen.blocksRaycasts = true;
        }

        private void UpdateIndicatorContent(
            TabBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            if (_selectionIcon != null)
            {
                _selectionIcon.sprite =
                    binding.SelectedIcon;

                _selectionIcon.enabled =
                    binding.SelectedIcon != null;
            }

            if (_selectionLabel != null)
            {
                _selectionLabel.text =
                    binding.Label;
            }
        }

        private static void SetNormalIconVisible(
            TabBinding binding,
            bool visible)
        {
            if (binding?.NormalIcon == null)
            {
                return;
            }

            binding.NormalIcon.enabled =
                visible;
        }

        private static void SetScreenImmediately(
            TabBinding binding,
            bool visible)
        {
            if (binding?.Screen == null ||
                binding.ScreenRect == null)
            {
                return;
            }

            binding.ScreenRect.gameObject.SetActive(true);

            binding.Screen.alpha =
                visible ? 1f : 0f;

            binding.Screen.interactable =
                visible;

            binding.Screen.blocksRaycasts =
                visible;

            binding.ScreenRect.anchoredPosition =
                binding.InitialAnchoredPosition;
        }

        public void ShowTabImmediately(
            BottomNavigationTab tab)
        {
            TabBinding binding =
                FindBinding(tab);

            if (binding == null)
            {
                Debug.LogError(
                    $"Bottom navigation tab {tab} is missing.",
                    this);

                return;
            }

            if (_currentTab != null &&
                !ReferenceEquals(
                    _currentTab,
                    binding))
            {
                SetNormalIconVisible(
                    _currentTab,
                    true);
            }

            for (int i = 0;
                 i < _tabs.Length;
                 i++)
            {
                bool selected =
                    ReferenceEquals(
                        _tabs[i],
                        binding);

                SetScreenImmediately(
                    _tabs[i],
                    selected);

                SetNormalIconVisible(
                    _tabs[i],
                    !selected);
            }

            BringScreenToFront(binding);

            MoveIndicatorImmediately(
                binding);

            UpdateIndicatorContent(
                binding);

            _currentTab =
                binding;

            _selectionAnimator.SetBool(
                IsSelectedHash,
                true);
        }

        private static void BringScreenToFront(
            TabBinding binding)
        {
            RectTransform screenRect =
                binding?.ScreenRect;

            if (screenRect == null)
            {
                return;
            }

            screenRect.SetAsLastSibling();
        }

        private TabBinding FindBinding(
            BottomNavigationTab tab)
        {
            if (_tabs == null)
            {
                return null;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] != null &&
                    _tabs[i].Tab == tab)
                {
                    return _tabs[i];
                }
            }

            return null;
        }

        private bool HasRequiredReferences()
        {
            if (_selectionIndicator == null)
            {
                Debug.LogError(
                    "Bottom navigation SelectionIndicator " +
                    "is missing.",
                    this);

                return false;
            }

            if (_selectionAnimator == null)
            {
                Debug.LogError(
                    "Bottom navigation Animator is missing.",
                    this);

                return false;
            }

            if (_tabs == null ||
                _tabs.Length == 0)
            {
                Debug.LogError(
                    "Bottom navigation tabs are missing.",
                    this);

                return false;
            }

            return true;
        }
    }
}
