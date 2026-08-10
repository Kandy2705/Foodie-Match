using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.UI.MainMenu;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardView :
        MonoBehaviour,
        IMainMenuTabSelectionHandler
    {
        private const float VietnamUtcOffsetHours = 7f;
        private const float RewardPreviewDuration = 3f;
        private const float RewardPreviewCloseDuration = 0.5f;
        private const int MaximumDisplayedPlayers = 99;
        private const int MedalRankCount = 3;
        private const string WeeklyValueLabel = "Score";
        private const string GlobalValueLabel = "Level";
        private const string WeeklyContentAddress =
            "Assets/FoodieMatch/Bundle/UI/LeaderBoardUI/WeeklyContent.prefab";
        private const string GlobalContentAddress =
            "Assets/FoodieMatch/Bundle/UI/LeaderBoardUI/GlobalContent.prefab";

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
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private TMP_Text _weeklyTimeRemainingText;

        [Header("Data Views")]
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
        [SerializeField, Min(0)] private int _virtualizationBufferRows = 2;

        [Header("Weekly Reveal")]
        private RectTransform[] _weeklyPodiumPlayers =
            Array.Empty<RectTransform>();
        private CanvasGroup[] _weeklyPodiumPlayerCanvasGroups =
            Array.Empty<CanvasGroup>();
        private RectTransform[] _weeklyRows = Array.Empty<RectTransform>();
        private CanvasGroup[] _weeklyRowCanvasGroups =
            Array.Empty<CanvasGroup>();
        private RectTransform[] _globalRows = Array.Empty<RectTransform>();
        private CanvasGroup[] _globalRowCanvasGroups =
            Array.Empty<CanvasGroup>();

        [Header("Current Player Reveal")]
        [SerializeField] private RectTransform _currentPlayerRow;
        [SerializeField] private CanvasGroup _currentPlayerCanvasGroup;

        [Header("Reveal Settings")]
        [SerializeField, Min(0f)] private float _revealDelay = 0.05f;

        [SerializeField, Min(0f)]
        private float _podiumRevealDuration = 0.45f;

        [SerializeField, Min(0f)]
        private float _rowStagger = 0.07f;

        [SerializeField, Min(0f)]
        private float _rowRevealDuration = 0.28f;

        private LeaderBoardTab _selectedTab;
        private readonly Dictionary<string, Sprite> _avatarsById =
            new(StringComparer.Ordinal);
        private LeaderBoardPlayerData _currentPlayer;
        private LeaderBoardPlayerData[] _weeklyPlayers;
        private LeaderBoardPlayerData[] _globalPlayers;
        private readonly VirtualizedListState _weeklyList = new();
        private readonly VirtualizedListState _globalList = new();
        private ScrollRect _weeklyScrollRect;
        private VerticalLayoutGroup _weeklyLayoutGroup;
        private ContentSizeFitter _weeklyContentSizeFitter;
        private ScrollRect _globalScrollRect;
        private VerticalLayoutGroup _globalLayoutGroup;
        private ContentSizeFitter _globalContentSizeFitter;
        private AsyncOperationHandle<GameObject> _activeContentHandle;
        private bool _hasActiveContentHandle;
        private bool _isContentLoading;
        private bool _isDestroyed;
        private int _contentRequestVersion;
        private LeaderBoardTab _loadingTab;
        private LeaderBoardTab _loadedContentTab;
        private RectTransform _currentPlayerStickyParent;
        private Vector3 _currentPlayerStickyLocalPosition;
        private Vector2 _currentPlayerStickyAnchorMin;
        private Vector2 _currentPlayerStickyAnchorMax;
        private Vector2 _currentPlayerStickyPivot;
        private Vector2 _currentPlayerStickySizeDelta;
        private Vector2 _currentPlayerStickyAnchoredPosition;
        private bool _isCurrentPlayerDocked;
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
            InitializeRewardPreviewPool();
            HideRewardPreview();
            CaptureCurrentPlayerStickyLayout();

            LoadData();
            _selectedTab = _initialTab;
            SetTabVisuals(_selectedTab);
            BindCurrentPlayer(_selectedTab);
            UpdateWeeklyTimeRemaining();
        }

        private void OnEnable()
        {
            UpdateWeeklyTimeRemaining();
            _ = EnsureTabContentLoadedAsync(
                _selectedTab,
                true);
        }

        private void OnDisable()
        {
            HideRewardPreview();
            StopRevealAnimation();
            _contentRequestVersion++;
            _isContentLoading = false;
            ReleaseActiveContent();
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
            _isDestroyed = true;
            _contentRequestVersion++;
            _isContentLoading = false;
            StopRevealAnimation();
            HideRewardPreview();
            ReleaseActiveContent();
            _weeklyButton.onClick.RemoveListener(OnWeeklyButtonClicked);
            _globalButton.onClick.RemoveListener(OnGlobalButtonClicked);
        }

        public void OnTabSelected()
        {
            _ = EnsureTabContentLoadedAsync(
                _selectedTab,
                true);
        }

        private async void OnWeeklyButtonClicked()
        {
            await SelectTabAsync(LeaderBoardTab.Weekly, false);
        }

        private async void OnGlobalButtonClicked()
        {
            await SelectTabAsync(LeaderBoardTab.Global, false);
        }

        private async Task SelectTabAsync(
            LeaderBoardTab tab,
            bool restartReveal)
        {
            if (!restartReveal &&
                tab == _selectedTab &&
                (_hasActiveContentHandle ||
                 (_isContentLoading && _loadingTab == tab)))
            {
                return;
            }

            HideRewardPreview();
            StopRevealAnimation();
            RestoreRevealTargets();

            _selectedTab = tab;
            SetTabVisuals(tab);
            BindCurrentPlayer(tab);
            await EnsureTabContentLoadedAsync(tab, restartReveal);
        }

        private void SetTabVisuals(LeaderBoardTab tab)
        {
            bool isWeekly = tab == LeaderBoardTab.Weekly;

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

        private async Task EnsureTabContentLoadedAsync(
            LeaderBoardTab tab,
            bool restartReveal)
        {
            if (_isDestroyed || !isActiveAndEnabled)
            {
                return;
            }

            if (_hasActiveContentHandle &&
                _loadedContentTab == tab)
            {
                if (restartReveal)
                {
                    ActivateLoadedContent(tab);
                }

                return;
            }

            if (_isContentLoading && _loadingTab == tab)
            {
                return;
            }

            int requestVersion = ++_contentRequestVersion;
            _isContentLoading = true;
            _loadingTab = tab;
            ReleaseActiveContent();

            string address =
                tab == LeaderBoardTab.Weekly
                    ? WeeklyContentAddress
                    : GlobalContentAddress;
            AsyncOperationHandle<GameObject> handle =
                Addressables.InstantiateAsync(
                    address,
                    _contentRoot,
                    instantiateInWorldSpace: false,
                    trackHandle: true);

            try
            {
                GameObject instance = await handle.Task;

                if (_isDestroyed ||
                    !isActiveAndEnabled ||
                    requestVersion != _contentRequestVersion ||
                    tab != _selectedTab)
                {
                    ReleaseContentHandle(handle);
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    instance == null)
                {
                    throw handle.OperationException ??
                        new InvalidOperationException(
                            $"Leaderboard content could not load: {address}");
                }

                _activeContentHandle = handle;
                _hasActiveContentHandle = true;
                _loadedContentTab = tab;
                BindLoadedContent(tab, instance);
                ActivateLoadedContent(tab);
            }
            catch (Exception exception)
            {
                if (_hasActiveContentHandle &&
                    _activeContentHandle.Equals(handle))
                {
                    ReleaseActiveContent();
                }
                else if (handle.IsValid())
                {
                    ReleaseContentHandle(handle);
                }

                Debug.LogError(
                    $"Failed to load leaderboard {tab} content: " +
                    exception);
            }
            finally
            {
                if (requestVersion == _contentRequestVersion)
                {
                    _isContentLoading = false;
                }
            }
        }

        private void BindLoadedContent(
            LeaderBoardTab tab,
            GameObject instance)
        {
            LeaderBoardContentView contentView =
                instance.GetComponent<LeaderBoardContentView>();

            if (contentView == null)
            {
                throw new MissingComponentException(
                    $"{instance.name} needs {nameof(LeaderBoardContentView)}.");
            }

            instance.transform.SetAsFirstSibling();
            instance.SetActive(true);

            if (tab == LeaderBoardTab.Weekly)
            {
                _weeklyScrollRect = contentView.ScrollRect;
                _weeklyLayoutGroup = contentView.LayoutGroup;
                _weeklyContentSizeFitter =
                    contentView.ContentSizeFitter;
                CachePodiumRevealTargets(
                    contentView.PodiumPlayers);
                ConfigureListState(
                    _weeklyList,
                    contentView.PlayerRows,
                    _weeklyPlayers);
                BindPodium(
                    contentView.PodiumPlayers,
                    _weeklyPlayers);
                _weeklyScrollRect.onValueChanged.AddListener(
                    OnWeeklyScrolled);
                _weeklyScrollDragRelay =
                    InitializeScrollDragRelay(_weeklyScrollRect);
            }
            else
            {
                _globalScrollRect = contentView.ScrollRect;
                _globalLayoutGroup = contentView.LayoutGroup;
                _globalContentSizeFitter =
                    contentView.ContentSizeFitter;
                ConfigureListState(
                    _globalList,
                    contentView.PlayerRows,
                    _globalPlayers);
                _globalScrollRect.onValueChanged.AddListener(
                    OnGlobalScrolled);
                _globalScrollDragRelay =
                    InitializeScrollDragRelay(_globalScrollRect);
            }
        }

        private void ActivateLoadedContent(
    LeaderBoardTab tab)
        {
            StopRevealAnimation();

            ResetListToTop(tab);
            EnsureListInitialized(tab);
            UpdateCurrentPlayerDock(tab);
            BindCurrentPlayer(tab);
            UpdateWeeklyTimeRemaining();

            PrepareRevealTargets(tab);

            PlayRevealAnimation(tab, false);
        }

        private void ReleaseActiveContent()
        {
            if (!_hasActiveContentHandle)
            {
                return;
            }

            HideRewardPreview();
            StopRevealAnimation();
            UndockCurrentPlayer();

            if (_loadedContentTab == LeaderBoardTab.Weekly)
            {
                if (_weeklyScrollRect != null)
                {
                    _weeklyScrollRect.onValueChanged.RemoveListener(
                        OnWeeklyScrolled);
                }

                _weeklyScrollDragRelay?.SetBeginDragHandler(null);
                ResetContentReferences(LeaderBoardTab.Weekly);
            }
            else
            {
                if (_globalScrollRect != null)
                {
                    _globalScrollRect.onValueChanged.RemoveListener(
                        OnGlobalScrolled);
                }

                _globalScrollDragRelay?.SetBeginDragHandler(null);
                ResetContentReferences(LeaderBoardTab.Global);
            }

            ReleaseContentHandle(_activeContentHandle);
            _activeContentHandle = default;
            _hasActiveContentHandle = false;
        }

        private void ResetContentReferences(
            LeaderBoardTab tab)
        {
            VirtualizedListState list =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyList
                    : _globalList;
            list.MedalTemplate = null;
            list.NumberedTemplate = null;
            list.MedalRows = null;
            list.NumberedRows = null;
            list.NumberedRowIndices = null;
            list.IsInitialized = false;

            if (tab == LeaderBoardTab.Weekly)
            {
                _weeklyScrollRect = null;
                _weeklyLayoutGroup = null;
                _weeklyContentSizeFitter = null;
                _weeklyScrollDragRelay = null;
                _weeklyPodiumPlayers =
                    Array.Empty<RectTransform>();
                _weeklyPodiumPlayerCanvasGroups =
                    Array.Empty<CanvasGroup>();
                _weeklyRows = Array.Empty<RectTransform>();
                _weeklyRowCanvasGroups =
                    Array.Empty<CanvasGroup>();
            }
            else
            {
                _globalScrollRect = null;
                _globalLayoutGroup = null;
                _globalContentSizeFitter = null;
                _globalScrollDragRelay = null;
                _globalRows = Array.Empty<RectTransform>();
                _globalRowCanvasGroups =
                    Array.Empty<CanvasGroup>();
            }
        }

        private static void ReleaseContentHandle(
            AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded &&
                handle.Result != null)
            {
                Addressables.ReleaseInstance(handle);
            }
            else
            {
                Addressables.Release(handle);
            }
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

        private void LoadData()
        {
            BuildAvatarLookup();

            LeaderBoardDataLoader loader = new();
            LeaderBoardDatabase database = loader.Load();
            _currentPlayer = loader.FindCurrentPlayer(database);

            _weeklyPlayers =
                GetRankedPlayers(
                    database.players,
                    (left, right) =>
                        right.weeklyScore.CompareTo(
                            left.weeklyScore));
            _globalPlayers =
                GetRankedPlayers(
                    database.players,
                    (left, right) =>
                        right.level.CompareTo(left.level));

            AssignRanks(
                _weeklyPlayers,
                player => player.weeklyScore,
                (player, rank) => player.weeklyRank = rank);
            AssignRanks(
                _globalPlayers,
                player => player.level,
                (player, rank) => player.globalRank = rank);

            _weeklyList.Players = _weeklyPlayers;
            _globalList.Players = _globalPlayers;
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
            LeaderBoardPodiumPlayerView[] podiumPlayers,
            LeaderBoardPlayerData[] weeklyPlayers)
        {
            for (int i = 0; i < podiumPlayers.Length; i++)
            {
                bool hasPlayer = i < weeklyPlayers.Length;
                podiumPlayers[i].gameObject.SetActive(hasPlayer);

                if (hasPlayer)
                {
                    LeaderBoardPlayerData player = weeklyPlayers[i];
                    podiumPlayers[i].Bind(
                        player,
                        GetAvatar(player.avatarId));
                }
            }
        }

        private void CachePodiumRevealTargets(
            LeaderBoardPodiumPlayerView[] podiumPlayers)
        {
            if (podiumPlayers == null ||
                podiumPlayers.Length == 0)
            {
                _weeklyPodiumPlayers =
                    Array.Empty<RectTransform>();
                _weeklyPodiumPlayerCanvasGroups =
                    Array.Empty<CanvasGroup>();
                return;
            }

            int count = podiumPlayers.Length;
            _weeklyPodiumPlayers =
                new RectTransform[count];
            _weeklyPodiumPlayerCanvasGroups =
                new CanvasGroup[count];

            for (int i = 0; i < count; i++)
            {
                LeaderBoardPodiumPlayerView podiumPlayer =
                    podiumPlayers[i];

                if (podiumPlayer == null)
                {
                    continue;
                }

                _weeklyPodiumPlayers[i] =
                    podiumPlayer.transform as RectTransform;

                CanvasGroup canvasGroup =
                    podiumPlayer.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup =
                        podiumPlayer.gameObject
                            .AddComponent<CanvasGroup>();
                }

                _weeklyPodiumPlayerCanvasGroups[i] =
                    canvasGroup;
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
            int currentPlayerIndex =
                GetCurrentPlayerIndex(list.Players);

            if (currentPlayerIndex >= rowCount)
            {
                contentHeight +=
                    list.RowHeight + layoutGroup.spacing;
            }
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

                if (!hasPlayer)
                {
                    list.NumberedRowIndices[i] = -1;
                }

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

            UpdateCurrentPlayerDock(tab);
            UpdateCurrentPlayerRowVisibility(list);
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

        private void MatchCurrentPlayerRowToList(
            VirtualizedListState list,
            ScrollRect scrollRect)
        {
            if (_currentPlayerRow == null || scrollRect == null)
            {
                return;
            }

            RectTransform referenceRow = null;

            if (list.NumberedTemplate != null)
            {
                referenceRow =
                    (RectTransform)list.NumberedTemplate.transform;
            }
            else if (list.MedalTemplate != null)
            {
                referenceRow =
                    (RectTransform)list.MedalTemplate.transform;
            }

            float targetWidth =
                referenceRow != null
                    ? referenceRow.rect.width
                    : scrollRect.content.rect.width;

            _currentPlayerRow.localScale = Vector3.one;

            _currentPlayerRow.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                targetWidth);

            _currentPlayerRow.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                list.RowHeight);
        }

        private void CaptureCurrentPlayerStickyLayout()
        {
            _currentPlayerStickyParent =
                _currentPlayerRow.parent as RectTransform;
            _currentPlayerStickyLocalPosition =
                _currentPlayerRow.localPosition;
            _currentPlayerStickyAnchorMin =
                _currentPlayerRow.anchorMin;
            _currentPlayerStickyAnchorMax =
                _currentPlayerRow.anchorMax;
            _currentPlayerStickyPivot =
                _currentPlayerRow.pivot;
            _currentPlayerStickySizeDelta =
                _currentPlayerRow.sizeDelta;
            _currentPlayerStickyAnchoredPosition =
                _currentPlayerRow.anchoredPosition;
        }

        private void UpdateCurrentPlayerDock(
            LeaderBoardTab tab)
        {
            VirtualizedListState list =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyList
                    : _globalList;
            ScrollRect scrollRect =
                tab == LeaderBoardTab.Weekly
                    ? _weeklyScrollRect
                    : _globalScrollRect;

            if (!list.IsInitialized ||
                scrollRect == null ||
                _currentPlayerStickyParent == null)
            {
                UndockCurrentPlayer();
                return;
            }

            int currentPlayerIndex =
                GetCurrentPlayerIndex(list.Players);

            if (currentPlayerIndex < 0)
            {
                UndockCurrentPlayer();
                return;
            }

            int rowCount =
                Mathf.Min(
                    list.Players.Length,
                    MaximumDisplayedPlayers);
            int inlineIndex =
                Mathf.Min(currentPlayerIndex, rowCount);
            float inlineY =
                -list.TopPadding -
                inlineIndex * list.RowStride -
                list.RowHeight * 0.5f;
            Vector3 inlineWorldPosition =
                scrollRect.content.TransformPoint(
                    new Vector3(0f, inlineY, 0f));
            Vector3 inlineInStickyParent =
                _currentPlayerStickyParent.InverseTransformPoint(
                    inlineWorldPosition);
            bool shouldDock =
                inlineInStickyParent.y >=
                _currentPlayerStickyLocalPosition.y;

            if (!shouldDock)
            {
                UndockCurrentPlayer();
                return;
            }

            if (!_isCurrentPlayerDocked)
            {
                _currentPlayerRow.SetParent(
                    scrollRect.content,
                    false);
                _isCurrentPlayerDocked = true;
            }

            PositionRow(
                _currentPlayerRow,
                inlineIndex,
                list.RowStride,
                list.RowHeight,
                list.TopPadding);
            MatchCurrentPlayerRowToList(
                list,
                scrollRect);
            _currentPlayerRow.SetAsLastSibling();
        }

        private void UndockCurrentPlayer()
        {
            if (!_isCurrentPlayerDocked ||
                _currentPlayerStickyParent == null)
            {
                return;
            }

            _currentPlayerRow.SetParent(
                _currentPlayerStickyParent,
                false);
            _currentPlayerRow.anchorMin =
                _currentPlayerStickyAnchorMin;
            _currentPlayerRow.anchorMax =
                _currentPlayerStickyAnchorMax;
            _currentPlayerRow.pivot =
                _currentPlayerStickyPivot;
            _currentPlayerRow.sizeDelta =
                _currentPlayerStickySizeDelta;
            _currentPlayerRow.anchoredPosition =
                _currentPlayerStickyAnchoredPosition;
            _currentPlayerRow.SetAsLastSibling();
            _isCurrentPlayerDocked = false;
        }

        private int GetCurrentPlayerIndex(
            LeaderBoardPlayerData[] players)
        {
            if (_currentPlayer == null || players == null)
            {
                return -1;
            }

            for (int i = 0; i < players.Length; i++)
            {
                LeaderBoardPlayerData player = players[i];

                if (ReferenceEquals(player, _currentPlayer) ||
                    (player.playerId == _currentPlayer.playerId &&
                     player.accountId == _currentPlayer.accountId))
                {
                    return i;
                }
            }

            return -1;
        }

        private void UpdateCurrentPlayerRowVisibility(
            VirtualizedListState list)
        {
            if (!list.IsInitialized)
            {
                return;
            }

            int currentPlayerIndex =
                GetCurrentPlayerIndex(list.Players);

            for (int i = 0; i < list.MedalRows.Length; i++)
            {
                bool shouldShow =
                    !_isCurrentPlayerDocked ||
                    currentPlayerIndex != i;
                list.MedalRows[i].gameObject.SetActive(shouldShow);
            }

            for (int i = 0; i < list.NumberedRows.Length; i++)
            {
                bool hasBoundPlayer =
                    list.NumberedRowIndices[i] >= 0;
                bool isCurrentPlayerRow =
                    list.NumberedRowIndices[i] ==
                    currentPlayerIndex;
                list.NumberedRows[i].gameObject.SetActive(
                    hasBoundPlayer &&
                    (!_isCurrentPlayerDocked ||
                     !isCurrentPlayerRow));
            }
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

        private void PlayRevealAnimation(
    LeaderBoardTab tab,
    bool prepareTargets = true)
        {
            if (prepareTargets)
            {
                PrepareRevealTargets(tab);
            }

            Sequence sequence =
                Sequence.Create(useUnscaledTime: true);

            float startTime = _revealDelay;

            if (tab == LeaderBoardTab.Weekly)
            {
                sequence = InsertPodiumPlayers(
                    sequence,
                    _weeklyPodiumPlayers,
                    _weeklyPodiumPlayerCanvasGroups,
                    startTime);

                sequence = InsertRows(
                    sequence,
                    _weeklyRows,
                    _weeklyRowCanvasGroups,
                    startTime);
            }
            else
            {
                sequence = InsertRows(
                    sequence,
                    _globalRows,
                    _globalRowCanvasGroups,
                    startTime);
            }

            sequence = InsertReveal(
                sequence,
                _currentPlayerRow,
                _currentPlayerCanvasGroup,
                startTime,
                _rowRevealDuration);

            _revealSequence = sequence;
        }

        private Sequence InsertPodiumPlayers(
            Sequence sequence,
            RectTransform[] players,
            CanvasGroup[] canvasGroups,
            float startTime)
        {
            int count = Mathf.Min(
                players.Length,
                canvasGroups.Length);

            for (int i = 0; i < count; i++)
            {
                sequence = InsertReveal(
                    sequence,
                    players[i],
                    canvasGroups[i],
                    startTime,
                    _podiumRevealDuration);
            }

            return sequence;
        }

        private Sequence InsertRows(
            Sequence sequence,
            RectTransform[] rows,
            CanvasGroup[] canvasGroups,
            float startTime)
        {
            int count = Mathf.Min(
                rows.Length,
                canvasGroups.Length);

            for (int i = 0; i < count; i++)
            {
                sequence = InsertReveal(
                    sequence,
                    rows[i],
                    canvasGroups[i],
                    startTime + _rowStagger * i,
                    _rowRevealDuration);
            }

            return sequence;
        }

        private Sequence InsertReveal(
            Sequence sequence,
            RectTransform target,
            CanvasGroup canvasGroup,
            float startTime,
            float duration)
        {
            if (target == null || canvasGroup == null)
            {
                return sequence;
            }

            return sequence
                .Insert(
                    startTime,
                    Tween.Scale(
                        target,
                        Vector3.one,
                        duration,
                        Ease.OutQuad))
                .Insert(
                    startTime,
                    Tween.Alpha(
                        canvasGroup,
                        0f,
                        1f,
                        duration,
                        Ease.Linear));
        }

        private void PrepareRevealTargets(LeaderBoardTab tab)
        {
            if (tab == LeaderBoardTab.Weekly)
            {
                PrepareRows(
                    _weeklyPodiumPlayers,
                    _weeklyPodiumPlayerCanvasGroups);
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
            if (rows == null || rowCanvasGroups == null)
            {
                return;
            }

            int count = Mathf.Min(
                rows.Length,
                rowCanvasGroups.Length);

            for (int i = 0; i < count; i++)
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
            if (target == null || canvasGroup == null)
            {
                return;
            }

            target.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }

        private void RestoreRevealTargets()
        {
            RestoreRows(
                _weeklyPodiumPlayers,
                _weeklyPodiumPlayerCanvasGroups);
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
            if (rows == null || rowCanvasGroups == null)
            {
                return;
            }

            int count = Mathf.Min(
                rows.Length,
                rowCanvasGroups.Length);

            for (int i = 0; i < count; i++)
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
            if (target == null || canvasGroup == null)
            {
                return;
            }

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

            if (_weeklyTimeRemainingText != null)
            {
                _weeklyTimeRemainingText.text =
                    $"{remaining.Days}d {remaining.Hours:00}h";
            }
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
