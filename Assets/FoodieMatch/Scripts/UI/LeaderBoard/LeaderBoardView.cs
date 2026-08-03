using System;
using System.Collections.Generic;
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
        private const int MaximumDisplayedPlayers = 100;
        private const int MedalRankCount = 3;
        private const string WeeklyValueLabel = "Score";
        private const string GlobalValueLabel = "Level";

        private enum LeaderBoardTab
        {
            Weekly,
            Global
        }

        [Serializable]
        private sealed class AvatarBinding
        {
            [SerializeField] private string _avatarId;
            [SerializeField] private Sprite _sprite;

            public string AvatarId => _avatarId;
            public Sprite Sprite => _sprite;
        }

        private sealed class VirtualizedListState
        {
            public LeaderBoardPlayerData[] Players;
            public LeaderBoardMedalPlayerRowView MedalTemplate;
            public LeaderBoardNumberedPlayerRowView NumberedTemplate;
            public LeaderBoardMedalPlayerRowView[] MedalRows;
            public LeaderBoardNumberedPlayerRowView[] NumberedRows;
            public int[] NumberedRowIndices;
            public bool IsInitialized;
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

        [Header("Data Views")]
        [SerializeField] private LeaderBoardPodiumPlayerView[] _weeklyPodiumPlayers;
        [SerializeField] private LeaderBoardPlayerRowView[] _weeklyPlayerRows;
        [SerializeField] private LeaderBoardPlayerRowView[] _globalPlayerRows;
        [SerializeField] private LeaderBoardCurrentPlayerView _currentPlayerView;
        [SerializeField] private AvatarBinding[] _avatarBindings;

        [Header("Virtualized Lists")]
        [SerializeField] private ScrollRect _weeklyScrollRect;
        [SerializeField] private VerticalLayoutGroup _weeklyLayoutGroup;
        [SerializeField] private ContentSizeFitter _weeklyContentSizeFitter;
        [SerializeField] private ScrollRect _globalScrollRect;
        [SerializeField] private VerticalLayoutGroup _globalLayoutGroup;
        [SerializeField] private ContentSizeFitter _globalContentSizeFitter;
        [SerializeField, Min(1f)] private float _rowHeight = 125f;
        [SerializeField, Min(0f)] private float _rowSpacing = 14f;
        [SerializeField, Min(0)] private int _virtualizationBufferRows = 2;

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
        private readonly Dictionary<string, Sprite> _avatarsById =
            new(StringComparer.Ordinal);
        private LeaderBoardPlayerData _currentPlayer;
        private readonly VirtualizedListState _weeklyList = new();
        private readonly VirtualizedListState _globalList = new();
        private Sequence _revealSequence;
        private float _nextTimerRefreshTime;

        private void Awake()
        {
            _weeklyButton.onClick.AddListener(OnWeeklyButtonClicked);
            _globalButton.onClick.AddListener(OnGlobalButtonClicked);
            _weeklyScrollRect.onValueChanged.AddListener(OnWeeklyScrolled);
            _globalScrollRect.onValueChanged.AddListener(OnGlobalScrolled);

            LoadAndBindData();
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
            _weeklyScrollRect.onValueChanged.RemoveListener(OnWeeklyScrolled);
            _globalScrollRect.onValueChanged.RemoveListener(OnGlobalScrolled);
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
            EnsureListInitialized(tab);
            _weeklyButton.interactable = !isWeekly;
            _globalButton.interactable = isWeekly;
            BindCurrentPlayer(tab);

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

        private void LoadAndBindData()
        {
            BuildAvatarLookup();

            LeaderBoardDataLoader loader = new();
            LeaderBoardDatabase database = loader.Load();
            _currentPlayer = loader.FindCurrentPlayer(database);

            LeaderBoardPlayerData[] weeklyPlayers =
                GetRankedPlayers(
                    database.players,
                    (left, right) =>
                        left.weeklyRank.CompareTo(right.weeklyRank));
            LeaderBoardPlayerData[] globalPlayers =
                GetRankedPlayers(
                    database.players,
                    (left, right) =>
                        left.globalRank.CompareTo(right.globalRank));

            BindPodium(weeklyPlayers);
            ConfigureListState(
                _weeklyList,
                _weeklyPlayerRows,
                weeklyPlayers);
            ConfigureListState(
                _globalList,
                _globalPlayerRows,
                globalPlayers);
        }

        private void BuildAvatarLookup()
        {
            for (int i = 0; i < _avatarBindings.Length; i++)
            {
                AvatarBinding binding = _avatarBindings[i];
                _avatarsById.Add(
                    binding.AvatarId,
                    binding.Sprite);
            }
        }

        private static LeaderBoardPlayerData[] GetRankedPlayers(
            LeaderBoardPlayerData[] players,
            Comparison<LeaderBoardPlayerData> comparison)
        {
            List<LeaderBoardPlayerData> rankedPlayers =
                new(players.Length);

            for (int i = 0; i < players.Length; i++)
            {
                rankedPlayers.Add(players[i]);
            }

            rankedPlayers.Sort(comparison);
            return rankedPlayers.ToArray();
        }

        private void BindPodium(
            LeaderBoardPlayerData[] weeklyPlayers)
        {
            for (int i = 0; i < _weeklyPodiumPlayers.Length; i++)
            {
                bool hasPlayer = i < weeklyPlayers.Length;
                _weeklyPodiumPlayers[i].gameObject.SetActive(hasPlayer);

                if (hasPlayer)
                {
                    LeaderBoardPlayerData player = weeklyPlayers[i];
                    _weeklyPodiumPlayers[i].Bind(
                        player,
                        GetAvatar(player.avatarId));
                }
            }
        }

        private static void ConfigureListState(
            VirtualizedListState list,
            LeaderBoardPlayerRowView[] templates,
            LeaderBoardPlayerData[] players)
        {
            list.Players = players;

            for (int i = 0; i < templates.Length; i++)
            {
                if (templates[i] is LeaderBoardMedalPlayerRowView medalRow)
                {
                    list.MedalTemplate = medalRow;
                }
                else if (
                    templates[i] is LeaderBoardNumberedPlayerRowView
                        numberedRow)
                {
                    list.NumberedTemplate = numberedRow;
                }

                templates[i].gameObject.SetActive(false);
            }
        }

        private void EnsureListInitialized(
            LeaderBoardTab tab)
        {
            VirtualizedListState list =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyList
                    : _globalList;

            if (list.IsInitialized)
            {
                RefreshVirtualizedList(tab, list);
                return;
            }

            ScrollRect scrollRect =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyScrollRect
                    : _globalScrollRect;
            VerticalLayoutGroup layoutGroup =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyLayoutGroup
                    : _globalLayoutGroup;
            ContentSizeFitter contentSizeFitter =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyContentSizeFitter
                    : _globalContentSizeFitter;

            layoutGroup.enabled = false;
            contentSizeFitter.enabled = false;
            Canvas.ForceUpdateCanvases();

            int rowCount =
                Mathf.Min(
                    list.Players.Length,
                    MaximumDisplayedPlayers);
            float rowStride = _rowHeight + _rowSpacing;
            float contentHeight =
                rowCount == 0
                    ? 0f
                    : rowCount * _rowHeight +
                      (rowCount - 1) * _rowSpacing;
            Vector2 contentSize = scrollRect.content.sizeDelta;
            contentSize.y = contentHeight;
            scrollRect.content.sizeDelta = contentSize;
            scrollRect.content.anchoredPosition = Vector2.zero;

            int medalRowCount =
                Mathf.Min(MedalRankCount, rowCount);
            list.MedalRows =
                new LeaderBoardMedalPlayerRowView[medalRowCount];

            for (int i = 0; i < medalRowCount; i++)
            {
                LeaderBoardMedalPlayerRowView row =
                    Instantiate(
                        list.MedalTemplate,
                        scrollRect.content);
                row.gameObject.SetActive(true);
                PositionRow(row.transform, i, rowStride);
                BindRow(row, list.Players[i], tab);
                list.MedalRows[i] = row;
            }

            int visibleRowCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        scrollRect.viewport.rect.height /
                        rowStride));
            int numberedPlayerCount =
                Mathf.Max(0, rowCount - MedalRankCount);
            int numberedPoolCount =
                Mathf.Min(
                    numberedPlayerCount,
                    visibleRowCount +
                    _virtualizationBufferRows * 2);
            list.NumberedRows =
                new LeaderBoardNumberedPlayerRowView[
                    numberedPoolCount];
            list.NumberedRowIndices = new int[numberedPoolCount];

            for (int i = 0; i < numberedPoolCount; i++)
            {
                LeaderBoardNumberedPlayerRowView row =
                    Instantiate(
                        list.NumberedTemplate,
                        scrollRect.content);
                row.gameObject.SetActive(false);
                list.NumberedRows[i] = row;
                list.NumberedRowIndices[i] = -1;
            }

            list.IsInitialized = true;
            RefreshVirtualizedList(tab, list);
            CacheRevealRows(tab, list);
        }

        private void OnWeeklyScrolled(
            Vector2 normalizedPosition)
        {
            RefreshVirtualizedList(
                LeaderBoardTab.Weekly,
                _weeklyList);
        }

        private void OnGlobalScrolled(
            Vector2 normalizedPosition)
        {
            RefreshVirtualizedList(
                LeaderBoardTab.Global,
                _globalList);
        }

        private void RefreshVirtualizedList(
            LeaderBoardTab tab,
            VirtualizedListState list)
        {
            if (!list.IsInitialized)
            {
                return;
            }

            ScrollRect scrollRect =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyScrollRect
                    : _globalScrollRect;
            float rowStride = _rowHeight + _rowSpacing;
            int rowCount =
                Mathf.Min(
                    list.Players.Length,
                    MaximumDisplayedPlayers);
            int firstVisibleIndex =
                Mathf.FloorToInt(
                    scrollRect.content.anchoredPosition.y /
                    rowStride);
            int firstNumberedIndex =
                Mathf.Clamp(
                    firstVisibleIndex -
                    _virtualizationBufferRows,
                    MedalRankCount,
                    Mathf.Max(MedalRankCount, rowCount));

            for (int i = 0; i < list.NumberedRows.Length; i++)
            {
                int playerIndex = firstNumberedIndex + i;
                LeaderBoardNumberedPlayerRowView row =
                    list.NumberedRows[i];
                bool hasPlayer = playerIndex < rowCount;
                row.gameObject.SetActive(hasPlayer);

                if (hasPlayer &&
                    list.NumberedRowIndices[i] != playerIndex)
                {
                    PositionRow(
                        row.transform,
                        playerIndex,
                        rowStride);
                    BindRow(
                        row,
                        list.Players[playerIndex],
                        tab);
                    list.NumberedRowIndices[i] = playerIndex;
                }
            }
        }

        private void PositionRow(
            Transform rowTransform,
            int playerIndex,
            float rowStride)
        {
            RectTransform row = (RectTransform)rowTransform;
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            Vector2 rowSize = row.sizeDelta;
            rowSize.y = _rowHeight;
            row.sizeDelta = rowSize;
            row.anchoredPosition =
                new Vector2(
                    0f,
                    -playerIndex * rowStride -
                    row.sizeDelta.y * 0.5f);
        }

        private void BindRow(
            LeaderBoardPlayerRowView row,
            LeaderBoardPlayerData player,
            LeaderBoardTab tab)
        {
            bool isWeekly = tab == LeaderBoardTab.Weekly;
            int rank =
                isWeekly
                    ? player.weeklyRank
                    : player.globalRank;

            row.gameObject.name = $"{tab}RankRow_{rank}";
            row.Bind(
                player,
                rank,
                isWeekly
                    ? WeeklyValueLabel
                    : GlobalValueLabel,
                isWeekly
                    ? player.weeklyScore
                    : player.level,
                GetAvatar(player.avatarId));
        }

        private void CacheRevealRows(
            LeaderBoardTab tab,
            VirtualizedListState list)
        {
            int rowCount =
                list.MedalRows.Length +
                list.NumberedRows.Length;
            RectTransform[] rows = new RectTransform[rowCount];
            CanvasGroup[] rowCanvasGroups =
                new CanvasGroup[rowCount];
            int targetIndex = 0;

            for (int i = 0; i < list.MedalRows.Length; i++)
            {
                rows[targetIndex] =
                    (RectTransform)list.MedalRows[i].transform;
                rowCanvasGroups[targetIndex] =
                    list.MedalRows[i]
                        .GetComponent<CanvasGroup>();
                targetIndex++;
            }

            for (int i = 0; i < list.NumberedRows.Length; i++)
            {
                rows[targetIndex] =
                    (RectTransform)list.NumberedRows[i].transform;
                rowCanvasGroups[targetIndex] =
                    list.NumberedRows[i]
                        .GetComponent<CanvasGroup>();
                targetIndex++;
            }

            if (tab == LeaderBoardTab.Weekly)
            {
                _weeklyRows = rows;
                _weeklyRowCanvasGroups = rowCanvasGroups;
            }
            else
            {
                _globalRows = rows;
                _globalRowCanvasGroups = rowCanvasGroups;
            }
        }

        private void BindCurrentPlayer(
            LeaderBoardTab tab)
        {
            bool isWeekly = tab == LeaderBoardTab.Weekly;

            _currentPlayerView.Bind(
                _currentPlayer,
                isWeekly
                    ? _currentPlayer.weeklyRank
                    : _currentPlayer.globalRank,
                isWeekly
                    ? WeeklyValueLabel
                    : GlobalValueLabel,
                isWeekly
                    ? _currentPlayer.weeklyScore
                    : _currentPlayer.level,
                GetAvatar(_currentPlayer.avatarId));
        }

        private Sprite GetAvatar(
            string avatarId)
        {
            return _avatarsById[avatarId];
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
