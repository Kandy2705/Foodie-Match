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
            private CanvasGroup _screen;

            [NonSerialized]
            public UnityAction ClickAction;

            public BottomNavigationTab Tab => _tab;
            public Button Button => _button;
            public RectTransform Target => _target;
            public Image NormalIcon => _normalIcon;
            public Sprite SelectedIcon => _selectedIcon;
            public string Label => _label;
            public CanvasGroup Screen => _screen;
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

        [Header("Screen Fade")]
        [SerializeField]
        private float _screenFadeDuration = 0.15f;

        private TabBinding _currentTab;
        private bool _isTransitioning;

        private void OnEnable()
        {
            SubscribeButtons();
        }

        private void Start()
        {
            InitializeNavigation();
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

            for (int i = 0; i < _tabs.Length; i++)
            {
                TabBinding binding = _tabs[i];

                bool selected =
                    ReferenceEquals(
                        binding,
                        initialBinding);

                SetScreenImmediately(
                    binding.Screen,
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
            if (selectedBinding == null ||
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

            _currentTab =
                selectedBinding;

            _selectionAnimator.SetBool(
                IsSelectedHash,
                true);

            await SwitchScreenAsync(
                previousBinding?.Screen,
                selectedBinding.Screen);
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

        private async Task SwitchScreenAsync(
            CanvasGroup previousScreen,
            CanvasGroup selectedScreen)
        {
            if (ReferenceEquals(
                    previousScreen,
                    selectedScreen))
            {
                return;
            }

            if (previousScreen != null)
            {
                previousScreen.interactable =
                    false;

                previousScreen.blocksRaycasts =
                    false;

                await FadeScreenAsync(
                    previousScreen,
                    0f,
                    _screenFadeDuration);

                previousScreen
                    .gameObject
                    .SetActive(false);
            }

            if (selectedScreen == null)
            {
                return;
            }

            selectedScreen
                .gameObject
                .SetActive(true);

            selectedScreen.alpha = 0f;
            selectedScreen.interactable = false;
            selectedScreen.blocksRaycasts = false;

            await FadeScreenAsync(
                selectedScreen,
                1f,
                _screenFadeDuration);

            selectedScreen.interactable = true;
            selectedScreen.blocksRaycasts = true;
        }

        private static async Task FadeScreenAsync(
            CanvasGroup canvasGroup,
            float targetAlpha,
            float duration)
        {
            if (canvasGroup == null)
            {
                return;
            }

            float startAlpha =
                canvasGroup.alpha;

            if (duration <= 0f ||
                Mathf.Approximately(
                    startAlpha,
                    targetAlpha))
            {
                canvasGroup.alpha =
                    targetAlpha;

                return;
            }

            await Tween.Custom(
                startValue: startAlpha,
                endValue: targetAlpha,
                duration: duration,
                onValueChange: value =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha =
                            value;
                    }
                },
                ease: Ease.Linear);

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    targetAlpha;
            }
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
            CanvasGroup screen,
            bool visible)
        {
            if (screen == null)
            {
                return;
            }

            screen.gameObject.SetActive(
                visible);

            screen.alpha =
                visible ? 1f : 0f;

            screen.interactable =
                visible;

            screen.blocksRaycasts =
                visible;
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
