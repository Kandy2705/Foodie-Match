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
        private const float RewardPreviewDuration = 3f;
        private const float RewardPreviewCloseDuration = 0.5f;
        private const int MaximumDisplayedPlayers = 99;
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
            public float RowHeight;
            public float RowStride;
            public int TopPadding;
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

        [Header("Reward Preview")]
        [SerializeField] private RectTransform _rewardPreviewPanel;
        [SerializeField] private Animator _rewardPreviewAnimator;
        [SerializeField] private RectTransform _rewardItemsContainer;
        [SerializeField] private RewardItemView _rewardItemPrefab;
        [SerializeField] private Sprite _coinRewardSprite;
        [SerializeField] private Sprite _plusOneRewardSprite;
        [SerializeField] private Sprite _brownBagRewardSprite;
        [SerializeField] private Sprite _freezeRewardSprite;
        [SerializeField] private Sprite _shuffleRewardSprite;
        [SerializeField] private Sprite _unlimitedHeartRewardSprite;

        [Header("Virtualized Lists")]
        [SerializeField] private ScrollRect _weeklyScrollRect;
        [SerializeField] private VerticalLayoutGroup _weeklyLayoutGroup;
        [SerializeField] private ContentSizeFitter _weeklyContentSizeFitter;
        [SerializeField] private ScrollRect _globalScrollRect;
        [SerializeField] private VerticalLayoutGroup _globalLayoutGroup;
        [SerializeField] private ContentSizeFitter _globalContentSizeFitter;
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
        private Tween _rewardPreviewHideTween;
        private Tween _rewardPreviewDeactivateTween;
        private readonly List<RewardItemView> _rewardItemPool = new();
        private LeaderBoardPlayerRowView _rewardPreviewSourceRow;
        private RectTransform _rewardPreviewSourceGift;
        private HorizontalLayoutGroup _rewardItemsLayoutGroup;
        private LeaderBoardScrollDragRelay _weeklyScrollDragRelay;
        private LeaderBoardScrollDragRelay _globalScrollDragRelay;
        private float _rewardPanelHorizontalPadding;
        private bool _rewardPreviewIsClosing;
        private float _nextTimerRefreshTime;

        private void Awake()
        {
            _weeklyButton.onClick.AddListener(OnWeeklyButtonClicked);
            _globalButton.onClick.AddListener(OnGlobalButtonClicked);
            _weeklyScrollRect.onValueChanged.AddListener(OnWeeklyScrolled);
            _globalScrollRect.onValueChanged.AddListener(OnGlobalScrolled);
            _weeklyScrollDragRelay =
                InitializeScrollDragRelay(_weeklyScrollRect);
            _globalScrollDragRelay =
                InitializeScrollDragRelay(_globalScrollRect);
            InitializeRewardPreviewPool();
            HideRewardPreview();

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

        private void OnDisable()
        {
            HideRewardPreview();
        }

        private void Update()
        {
            if (!_rewardPreviewIsClosing &&
                _rewardPreviewPanel != null &&
                _rewardPreviewPanel.gameObject.activeSelf)
            {
                if (_rewardPreviewSourceGift == null ||
                    !_rewardPreviewSourceGift.gameObject
                        .activeInHierarchy)
                {
                    HideRewardPreview();
                }
                else
                {
                    PositionRewardPreview(
                        _rewardPreviewSourceGift);
                }
            }

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
            HideRewardPreview();
            _weeklyButton.onClick.RemoveListener(OnWeeklyButtonClicked);
            _globalButton.onClick.RemoveListener(OnGlobalButtonClicked);
            _weeklyScrollRect.onValueChanged.RemoveListener(OnWeeklyScrolled);
            _globalScrollRect.onValueChanged.RemoveListener(OnGlobalScrolled);
            _weeklyScrollDragRelay?.SetBeginDragHandler(null);
            _globalScrollDragRelay?.SetBeginDragHandler(null);
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

            HideRewardPreview();
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
            ResetListToTop(tab);
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

        private void ResetListToTop(
            LeaderBoardTab tab)
        {
            ScrollRect scrollRect =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyScrollRect
                    : _globalScrollRect;
            Vector2 contentPosition =
                scrollRect.content.anchoredPosition;

            scrollRect.StopMovement();
            contentPosition.y = 0f;
            scrollRect.content.anchoredPosition = contentPosition;
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
                        right.weeklyScore.CompareTo(
                            left.weeklyScore));
            LeaderBoardPlayerData[] globalPlayers =
                GetRankedPlayers(
                    database.players,
                    (left, right) =>
                        right.level.CompareTo(left.level));

            AssignRanks(
                weeklyPlayers,
                player => player.weeklyScore,
                (player, rank) => player.weeklyRank = rank);
            AssignRanks(
                globalPlayers,
                player => player.level,
                (player, rank) => player.globalRank = rank);

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

        private static void AssignRanks(
            LeaderBoardPlayerData[] players,
            Func<LeaderBoardPlayerData, int> getValue,
            Action<LeaderBoardPlayerData, int> setRank)
        {
            int previousValue = getValue(players[0]);
            int rank = 1;

            for (int i = 0; i < players.Length; i++)
            {
                int value = getValue(players[i]);

                if (value != previousValue)
                {
                    rank = i + 1;
                }

                setRank(players[i], rank);
                previousValue = value;
            }
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
            list.RowHeight =
                ((RectTransform)list.NumberedTemplate.transform)
                    .sizeDelta.y;
            list.RowStride =
                list.RowHeight + layoutGroup.spacing;
            list.TopPadding = layoutGroup.padding.top;
            float contentHeight =
                rowCount == 0
                    ? 0f
                    : layoutGroup.padding.top +
                      rowCount * list.RowHeight +
                      (rowCount - 1) * layoutGroup.spacing +
                      layoutGroup.padding.bottom;
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
                PositionRow(
                    row.transform,
                    i,
                    list.RowStride,
                    list.RowHeight,
                    list.TopPadding);
                BindRow(row, list.Players[i], tab);
                list.MedalRows[i] = row;
            }

            int visibleRowCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        scrollRect.viewport.rect.height /
                        list.RowStride));
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

        private LeaderBoardScrollDragRelay InitializeScrollDragRelay(
            ScrollRect scrollRect)
        {
            LeaderBoardScrollDragRelay relay =
                scrollRect.GetComponent<LeaderBoardScrollDragRelay>();

            if (relay == null)
            {
                relay = scrollRect.gameObject
                    .AddComponent<LeaderBoardScrollDragRelay>();
            }

            relay.SetBeginDragHandler(HideRewardPreviewOnScroll);
            return relay;
        }

        private void HideRewardPreviewOnScroll()
        {
            if (_rewardPreviewPanel != null &&
                _rewardPreviewPanel.gameObject.activeSelf &&
                !_rewardPreviewIsClosing)
            {
                BeginHideRewardPreview();
            }
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
            int rowCount =
                Mathf.Min(
                    list.Players.Length,
                    MaximumDisplayedPlayers);
            int firstVisibleIndex =
                Mathf.FloorToInt(
                    scrollRect.content.anchoredPosition.y /
                    list.RowStride);
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

                if (!hasPlayer &&
                    ReferenceEquals(_rewardPreviewSourceRow, row))
                {
                    HideRewardPreview();
                }

                row.gameObject.SetActive(hasPlayer);

                if (hasPlayer &&
                    list.NumberedRowIndices[i] != playerIndex)
                {
                    PositionRow(
                        row.transform,
                        playerIndex,
                        list.RowStride,
                        list.RowHeight,
                        list.TopPadding);
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
            float rowStride,
            float rowHeight,
            int topPadding)
        {
            RectTransform row = (RectTransform)rowTransform;
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            Vector2 rowSize = row.sizeDelta;
            rowSize.y = rowHeight;
            row.sizeDelta = rowSize;
            row.anchoredPosition =
                new Vector2(
                    0f,
                    -topPadding -
                    playerIndex * rowStride -
                    row.sizeDelta.y * 0.5f);
        }

        private void BindRow(
            LeaderBoardPlayerRowView row,
            LeaderBoardPlayerData player,
            LeaderBoardTab tab)
        {
            if (ReferenceEquals(_rewardPreviewSourceRow, row))
            {
                HideRewardPreview();
            }

            bool isWeekly = tab == LeaderBoardTab.Weekly;
            int rank =
                isWeekly
                    ? player.weeklyRank
                    : player.globalRank;

            row.gameObject.name = $"{tab}RankRow_{rank}";
            row.SetGiftClickHandler(OnGiftClicked);
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

            row.HideGift();

            if (isWeekly &&
                row is LeaderBoardMedalPlayerRowView medalRow)
            {
                medalRow.ShowWeeklyGift(rank);
            }
        }

        private void OnGiftClicked(
            LeaderBoardPlayerRowView sourceRow,
            RectTransform giftRectTransform,
            int rank)
        {
            if (sourceRow == null ||
                giftRectTransform == null ||
                _rewardPreviewPanel == null ||
                rank < 1 ||
                rank > MedalRankCount)
            {
                HideRewardPreview();
                return;
            }

            bool isSameGiftOpen =
                !_rewardPreviewIsClosing &&
                _rewardPreviewPanel.gameObject.activeSelf &&
                ReferenceEquals(
                    _rewardPreviewSourceRow,
                    sourceRow) &&
                ReferenceEquals(
                    _rewardPreviewSourceGift,
                    giftRectTransform);

            if (isSameGiftOpen)
            {
                BeginHideRewardPreview();
                return;
            }

            StopRewardPreviewTimer();
            StopRewardPreviewDeactivateTimer();
            _rewardPreviewIsClosing = false;
            _rewardPreviewSourceRow = sourceRow;
            _rewardPreviewSourceGift = giftRectTransform;
            PopulateRewardPreview(rank);

            if (!PositionRewardPreview(giftRectTransform))
            {
                HideRewardPreview();
                return;
            }

            _rewardPreviewPanel.SetAsLastSibling();
            _rewardPreviewPanel.gameObject.SetActive(true);
            PlayRewardPreviewOpenAnimation();

            _rewardPreviewHideTween = Tween.Delay(
                this,
                RewardPreviewDuration,
                view => view.BeginHideRewardPreview(),
                useUnscaledTime: true);
        }

        private void PlayRewardPreviewOpenAnimation()
        {
            if (!PrepareRewardPreviewAnimator())
            {
                return;
            }

            _rewardPreviewAnimator.ResetTrigger("Close");
            _rewardPreviewAnimator.ResetTrigger("Open");
            _rewardPreviewAnimator.Play("Normal", 0, 0f);
            _rewardPreviewAnimator.Update(0f);
            _rewardPreviewAnimator.SetTrigger("Open");
        }

        private void BeginHideRewardPreview()
        {
            StopRewardPreviewTimer();

            if (_rewardPreviewIsClosing)
            {
                return;
            }

            if (_rewardPreviewPanel == null ||
                !_rewardPreviewPanel.gameObject.activeSelf)
            {
                HideRewardPreview();
                return;
            }

            if (!PrepareRewardPreviewAnimator())
            {
                HideRewardPreview();
                return;
            }

            StopRewardPreviewDeactivateTimer();
            _rewardPreviewIsClosing = true;
            _rewardPreviewAnimator.ResetTrigger("Open");
            _rewardPreviewAnimator.ResetTrigger("Close");
            _rewardPreviewAnimator.SetTrigger("Close");
            _rewardPreviewDeactivateTween = Tween.Delay(
                this,
                RewardPreviewCloseDuration,
                view => view.HideRewardPreview(),
                useUnscaledTime: true);
        }

        private bool PositionRewardPreview(
            RectTransform giftRectTransform)
        {
            if (_rewardPreviewPanel == null ||
                giftRectTransform == null ||
                _rewardPreviewPanel.parent is not
                    RectTransform panelParent)
            {
                return false;
            }

            Vector3 giftTopCenter =
                giftRectTransform.TransformPoint(
                    new Vector3(
                        giftRectTransform.rect.center.x,
                        giftRectTransform.rect.yMax,
                        0f));
            Vector2 localPosition =
                panelParent.InverseTransformPoint(giftTopCenter);
            localPosition.y +=
                _rewardPreviewPanel.rect.height *
                _rewardPreviewPanel.pivot.y + 20f;
            _rewardPreviewPanel.anchoredPosition = localPosition;
            return true;
        }

        private void InitializeRewardPreviewPool()
        {
            if (_rewardPreviewPanel == null ||
                _rewardItemsContainer == null)
            {
                return;
            }

            _rewardItemsLayoutGroup =
                _rewardItemsContainer
                    .GetComponent<HorizontalLayoutGroup>();

            if (_rewardItemsLayoutGroup != null)
            {
                float existingItemsWidth = 0f;
                int existingItemCount =
                    _rewardItemsContainer.childCount;

                for (int i = 0; i < existingItemCount; i++)
                {
                    if (_rewardItemsContainer.GetChild(i) is
                        RectTransform itemRect)
                    {
                        existingItemsWidth += itemRect.rect.width;
                    }
                }

                float existingSpacing =
                    Mathf.Max(0, existingItemCount - 1) *
                    _rewardItemsLayoutGroup.spacing;
                _rewardPanelHorizontalPadding =
                    Mathf.Max(
                        0f,
                        _rewardPreviewPanel.rect.width -
                        existingItemsWidth -
                        existingSpacing);
            }

            RewardItemView[] existingItems =
                _rewardItemsContainer
                    .GetComponentsInChildren<RewardItemView>(true);

            for (int i = 0; i < existingItems.Length; i++)
            {
                existingItems[i].gameObject.SetActive(false);
                _rewardItemPool.Add(existingItems[i]);
            }
        }

        private void PopulateRewardPreview(
            int rank)
        {
            for (int i = 0; i < _rewardItemPool.Count; i++)
            {
                _rewardItemPool[i].gameObject.SetActive(false);
            }

            int rewardCount = rank switch
            {
                1 => 5,
                2 => 3,
                3 => 2,
                _ => 0
            };

            EnsureRewardItemPoolSize(rewardCount);

            switch (rank)
            {
                case 1:
                    BindRewardItem(0, _coinRewardSprite, "2000");
                    BindRewardItem(1, _plusOneRewardSprite, "x1");
                    BindRewardItem(2, _brownBagRewardSprite, "x1");
                    BindRewardItem(3, _freezeRewardSprite, "x1");
                    BindRewardItem(4, _shuffleRewardSprite, "x1");
                    break;
                case 2:
                    BindRewardItem(0, _brownBagRewardSprite, "x1");
                    BindRewardItem(1, _coinRewardSprite, "500");
                    BindRewardItem(
                        2,
                        _unlimitedHeartRewardSprite,
                        "30m");
                    break;
                case 3:
                    BindRewardItem(0, _coinRewardSprite, "500");
                    BindRewardItem(
                        1,
                        _unlimitedHeartRewardSprite,
                        "30m");
                    break;
            }

            ResizeRewardPreviewPanel(rewardCount);
        }

        private void EnsureRewardItemPoolSize(
            int requiredCount)
        {
            if (_rewardItemPrefab == null ||
                _rewardItemsContainer == null)
            {
                return;
            }

            while (_rewardItemPool.Count < requiredCount)
            {
                RewardItemView item =
                    Instantiate(
                        _rewardItemPrefab,
                        _rewardItemsContainer);
                item.gameObject.SetActive(false);
                _rewardItemPool.Add(item);
            }
        }

        private void BindRewardItem(
            int index,
            Sprite icon,
            string amountText)
        {
            if (index < 0 || index >= _rewardItemPool.Count)
            {
                return;
            }

            _rewardItemPool[index].Bind(icon, amountText);
        }

        private void ResizeRewardPreviewPanel(
            int activeItemCount)
        {
            if (_rewardPreviewPanel == null ||
                _rewardItemsLayoutGroup == null ||
                _rewardItemPrefab == null ||
                activeItemCount <= 0)
            {
                return;
            }

            RectTransform itemRect =
                (RectTransform)_rewardItemPrefab.transform;
            float width =
                _rewardPanelHorizontalPadding +
                activeItemCount * itemRect.rect.width +
                Mathf.Max(0, activeItemCount - 1) *
                _rewardItemsLayoutGroup.spacing;
            Vector2 panelSize = _rewardPreviewPanel.sizeDelta;
            panelSize.x = width;
            _rewardPreviewPanel.sizeDelta = panelSize;
        }

        private void HideRewardPreview()
        {
            StopRewardPreviewTimer();
            StopRewardPreviewDeactivateTimer();
            _rewardPreviewIsClosing = false;
            _rewardPreviewSourceRow = null;
            _rewardPreviewSourceGift = null;

            if (CanControlRewardPreviewAnimator())
            {
                _rewardPreviewAnimator.ResetTrigger("Open");
                _rewardPreviewAnimator.ResetTrigger("Close");
            }

            if (_rewardPreviewPanel != null)
            {
                _rewardPreviewPanel.gameObject.SetActive(false);
            }
        }

        private bool PrepareRewardPreviewAnimator()
        {
            if (_rewardPreviewAnimator == null ||
                !_rewardPreviewAnimator.isActiveAndEnabled ||
                _rewardPreviewAnimator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (!_rewardPreviewAnimator.isInitialized)
            {
                _rewardPreviewAnimator.Rebind();
                _rewardPreviewAnimator.Update(0f);
            }

            return _rewardPreviewAnimator.isInitialized;
        }

        private bool CanControlRewardPreviewAnimator()
        {
            return _rewardPreviewAnimator != null &&
                   _rewardPreviewAnimator.isActiveAndEnabled &&
                   _rewardPreviewAnimator.isInitialized &&
                   _rewardPreviewAnimator
                       .runtimeAnimatorController != null;
        }

        private void StopRewardPreviewTimer()
        {
            if (_rewardPreviewHideTween.isAlive)
            {
                _rewardPreviewHideTween.Stop();
            }

            _rewardPreviewHideTween = default;
        }

        private void StopRewardPreviewDeactivateTimer()
        {
            if (_rewardPreviewDeactivateTween.isAlive)
            {
                _rewardPreviewDeactivateTween.Stop();
            }

            _rewardPreviewDeactivateTween = default;
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
