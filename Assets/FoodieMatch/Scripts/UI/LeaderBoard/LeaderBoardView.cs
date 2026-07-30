using System;
using FoodieMatch.UI.MainMenu;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardView :
        MonoBehaviour,
        IMainMenuTabSelectionHandler
    {
        private const float VietnamUtcOffsetHours = 7f;
        private const float ListOvershootScale = 1.1f;
        private const float FeaturedOvershootScale = 1.2f;

        private enum LeaderBoardTab
        {
            Weekly,
            Global
        }

        [Header("Tabs")]
        [SerializeField] private Button _weeklyButton;
        [SerializeField] private Button _globalButton;
        [SerializeField] private Image _weeklyButtonImage;
        [SerializeField] private Image _globalButtonImage;
        [SerializeField] private TMP_Text _weeklyButtonLabel;
        [SerializeField] private TMP_Text _globalButtonLabel;
        [SerializeField] private Sprite _selectedTabSprite;
        [SerializeField] private Sprite _unselectedTabSprite;
        [SerializeField] private LeaderBoardTab _initialTab = LeaderBoardTab.Weekly;

        [Header("Content")]
        [SerializeField] private GameObject _weeklyContent;
        [SerializeField] private GameObject _globalContent;
        [SerializeField] private TMP_Text _weeklyTimeRemainingText;

        [Header("Weekly Reveal")]
        [SerializeField] private RectTransform _weeklyPodiumRoot;
        [SerializeField] private CanvasGroup _weeklyPodiumCanvasGroup;
        [SerializeField] private RectTransform[] _weeklyRows;
        [SerializeField] private CanvasGroup[] _weeklyRowCanvasGroups;

        [Header("Global Reveal")]
        [SerializeField] private RectTransform[] _globalRows;
        [SerializeField] private CanvasGroup[] _globalRowCanvasGroups;

        [Header("Current Player Reveal")]
        [SerializeField] private RectTransform _currentPlayerRow;
        [SerializeField] private CanvasGroup _currentPlayerCanvasGroup;

        [Header("Reveal Settings")]
        [SerializeField, Min(0f)] private float _revealDelay = 0.08f;
        [SerializeField, Min(0f)] private float _rowStagger = 0.08f;
        [SerializeField, Min(0f)] private float _scaleUpDuration = 0.18f;
        [SerializeField, Min(0f)] private float _settleDuration = 0.12f;

        private LeaderBoardTab _selectedTab;
        private Sequence _revealSequence;
        private float _nextTimerRefreshTime;

        private void Awake()
        {
            _weeklyButton.onClick.AddListener(OnWeeklyButtonClicked);
            _globalButton.onClick.AddListener(OnGlobalButtonClicked);

            _selectedTab = _initialTab;
            SetTabContent(_selectedTab);
            RestoreRevealTargets();
            UpdateWeeklyTimeRemaining();
        }

        private void OnEnable()
        {
            UpdateWeeklyTimeRemaining();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextTimerRefreshTime)
            {
                return;
            }

            UpdateWeeklyTimeRemaining();
            _nextTimerRefreshTime = Time.unscaledTime + 1f;
        }

        private void OnDestroy()
        {
            StopRevealAnimation();
            _weeklyButton.onClick.RemoveListener(OnWeeklyButtonClicked);
            _globalButton.onClick.RemoveListener(OnGlobalButtonClicked);
        }

        public void OnTabSelected()
        {
            SelectTab(_selectedTab, true);
        }

        private void OnWeeklyButtonClicked()
        {
            SelectTab(LeaderBoardTab.Weekly, false);
        }

        private void OnGlobalButtonClicked()
        {
            SelectTab(LeaderBoardTab.Global, false);
        }

        private void SelectTab(
            LeaderBoardTab tab,
            bool restartReveal)
        {
            if (!restartReveal && tab == _selectedTab)
            {
                return;
            }

            StopRevealAnimation();
            RestoreRevealTargets();

            _selectedTab = tab;
            SetTabContent(tab);
            PlayRevealAnimation(tab);
        }

        private void SetTabContent(LeaderBoardTab tab)
        {
            bool isWeekly = tab == LeaderBoardTab.Weekly;

            _weeklyContent.SetActive(isWeekly);
            _globalContent.SetActive(!isWeekly);
            _weeklyButton.interactable = !isWeekly;
            _globalButton.interactable = isWeekly;

            SetButtonVisual(
                _weeklyButtonImage,
                _weeklyButtonLabel,
                isWeekly,
                false);
            SetButtonVisual(
                _globalButtonImage,
                _globalButtonLabel,
                !isWeekly,
                true);
        }

        private void SetButtonVisual(
            Image buttonImage,
            TMP_Text label,
            bool isSelected,
            bool isRightTab)
        {
            buttonImage.sprite =
                isSelected ? _selectedTabSprite : _unselectedTabSprite;

            bool shouldMirror =
                isSelected ? isRightTab : !isRightTab;
            float horizontalScale = shouldMirror ? -1f : 1f;

            Vector3 buttonScale = buttonImage.rectTransform.localScale;
            buttonScale.x = Mathf.Abs(buttonScale.x) * horizontalScale;
            buttonImage.rectTransform.localScale = buttonScale;

            Vector3 labelScale = label.rectTransform.localScale;
            labelScale.x = Mathf.Abs(labelScale.x);
            label.rectTransform.localScale = labelScale;
        }

        private void PlayRevealAnimation(LeaderBoardTab tab)
        {
            PrepareRevealTargets(tab);

            Sequence sequence = Sequence.Create(useUnscaledTime: true);

            float featuredStartTime = _revealDelay;
            float listStartTime = featuredStartTime;

            if (tab == LeaderBoardTab.Weekly)
            {
                sequence = InsertReveal(
                    sequence,
                    _weeklyPodiumRoot,
                    _weeklyPodiumCanvasGroup,
                    featuredStartTime,
                    FeaturedOvershootScale);

                sequence = InsertRows(
                    sequence,
                    _weeklyRows,
                    _weeklyRowCanvasGroups,
                    listStartTime);
            }
            else
            {
                sequence = InsertRows(
                    sequence,
                    _globalRows,
                    _globalRowCanvasGroups,
                    listStartTime);
            }

            sequence = InsertReveal(
                sequence,
                _currentPlayerRow,
                _currentPlayerCanvasGroup,
                featuredStartTime,
                FeaturedOvershootScale);

            _revealSequence = sequence;
        }

        private Sequence InsertRows(
            Sequence sequence,
            RectTransform[] rows,
            CanvasGroup[] rowCanvasGroups,
            float startTime)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                sequence = InsertReveal(
                    sequence,
                    rows[i],
                    rowCanvasGroups[i],
                    startTime + _rowStagger * i,
                    ListOvershootScale);
            }

            return sequence;
        }

        private Sequence InsertReveal(
            Sequence sequence,
            RectTransform target,
            CanvasGroup canvasGroup,
            float startTime,
            float overshootScale)
        {
            float revealDuration =
                _scaleUpDuration + _settleDuration;

            return sequence
                .Insert(
                    startTime,
                    Tween.Scale(
                        target,
                        Vector3.one * overshootScale,
                        _scaleUpDuration,
                        Ease.OutQuad))
                .Insert(
                    startTime + _scaleUpDuration,
                    Tween.Scale(
                        target,
                        Vector3.one,
                        _settleDuration,
                        Ease.OutBack))
                .Insert(
                    startTime,
                    Tween.Alpha(
                        canvasGroup,
                        0f,
                        1f,
                        revealDuration,
                        Ease.Linear));
        }

        private void PrepareRevealTargets(LeaderBoardTab tab)
        {
            if (tab == LeaderBoardTab.Weekly)
            {
                PrepareRevealTarget(
                    _weeklyPodiumRoot,
                    _weeklyPodiumCanvasGroup);
                PrepareRows(
                    _weeklyRows,
                    _weeklyRowCanvasGroups);
            }
            else
            {
                PrepareRows(
                    _globalRows,
                    _globalRowCanvasGroups);
            }

            PrepareRevealTarget(
                _currentPlayerRow,
                _currentPlayerCanvasGroup);
        }

        private static void PrepareRows(
            RectTransform[] rows,
            CanvasGroup[] rowCanvasGroups)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                PrepareRevealTarget(
                    rows[i],
                    rowCanvasGroups[i]);
            }
        }

        private static void PrepareRevealTarget(
            RectTransform target,
            CanvasGroup canvasGroup)
        {
            target.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }

        private void RestoreRevealTargets()
        {
            RestoreRevealTarget(
                _weeklyPodiumRoot,
                _weeklyPodiumCanvasGroup);
            RestoreRows(
                _weeklyRows,
                _weeklyRowCanvasGroups);
            RestoreRows(
                _globalRows,
                _globalRowCanvasGroups);
            RestoreRevealTarget(
                _currentPlayerRow,
                _currentPlayerCanvasGroup);
        }

        private static void RestoreRows(
            RectTransform[] rows,
            CanvasGroup[] rowCanvasGroups)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                RestoreRevealTarget(
                    rows[i],
                    rowCanvasGroups[i]);
            }
        }

        private static void RestoreRevealTarget(
            RectTransform target,
            CanvasGroup canvasGroup)
        {
            target.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        private void StopRevealAnimation()
        {
            if (_revealSequence.isAlive)
            {
                _revealSequence.Stop();
            }

            _revealSequence = default;
        }

        private void UpdateWeeklyTimeRemaining()
        {
            DateTimeOffset vietnamNow =
                DateTimeOffset.UtcNow.ToOffset(
                    TimeSpan.FromHours(VietnamUtcOffsetHours));
            TimeSpan remaining =
                GetNextWeeklyResetTime(vietnamNow) -
                vietnamNow;

            _weeklyTimeRemainingText.text =
                $"{remaining.Days}d {remaining.Hours:00}h";
        }

        private static DateTimeOffset GetNextWeeklyResetTime(
            DateTimeOffset vietnamNow)
        {
            TimeSpan vietnamOffset =
                TimeSpan.FromHours(VietnamUtcOffsetHours);
            int daysUntilMonday =
                ((int)DayOfWeek.Monday -
                 (int)vietnamNow.DayOfWeek +
                 7) % 7;

            DateTime targetDate =
                vietnamNow.Date.AddDays(daysUntilMonday);
            DateTimeOffset resetTime =
                new(
                    targetDate.Year,
                    targetDate.Month,
                    targetDate.Day,
                    0,
                    0,
                    0,
                    vietnamOffset);

            if (resetTime <= vietnamNow)
            {
                resetTime = resetTime.AddDays(7);
            }

            return resetTime;
        }
    }
}
