using System;
using System.Collections;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Rewards;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodieMatch.UI.DailyReward
{
    [DisallowMultipleComponent]
    public sealed class DailyRewardPopupView : PopupBase
    {
        private const float AvailableButtonPixelsPerUnitMultiplier = 1.5f;
        private const float LockedButtonPixelsPerUnitMultiplier = 1.0f;
        private const float ProgressCurrentPositionX = 12.12f;
        private const float ProgressDefaultPositionX = 0f;

        private static readonly string[] QuestTitles =
        {
            "Pass 3 levels",
            "Use 2 Storage",
            "Use 2 Refresh",
            "Use 2 Plate",
            "Use 2 Fridge"
        };

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _questsTabButton;
        [SerializeField] private Button _freeCoinTabButton;

        [Header("Content")]
        [SerializeField] private GameObject _questContent;
        [SerializeField] private GameObject _freeCoinContent;

        [Header("Scroll")]
        [SerializeField] private ScrollRect _questScrollRect;

        [Header("Tabs")]
        [SerializeField] private Image _questsTabImage;
        [SerializeField] private Image _freeCoinTabImage;

        [Header("Sprites")]
        [SerializeField] private Sprite _tabOnSprite;
        [SerializeField] private Sprite _tabOffSprite;
        [SerializeField] private Sprite _progressCurrentSprite;
        [SerializeField] private Sprite _progressFilledSprite;
        [SerializeField] private Sprite _progressCheckedSprite;

        [Header("Claimed State")]
        [SerializeField] private TMP_FontAsset _claimedFont;
        [SerializeField] private Color _claimedTextColor;

        [Header("Animation")]
        [SerializeField] private PopupAnimController _popupAnimController;

        [Header("Daily Gift Button Visuals")]
        [SerializeField] private Sprite _dailyGiftClaimSprite;
        [SerializeField] private Sprite _dailyGiftCooldownSprite;
        [SerializeField] private TMP_FontAsset _dailyGiftClaimFont;
        [SerializeField] private TMP_FontAsset _dailyGiftCooldownFont;
        [SerializeField] private float _dailyGiftCooldownImageTop = 8f;
        [SerializeField] private float _dailyGiftCooldownImageBottom = 8f;
        [SerializeField] private float _dailyGiftClaimPixelsPerUnitMultiplier = 1.5f;
        [SerializeField] private float _dailyGiftCooldownPixelsPerUnitMultiplier = 1.0f;

        [Header("Catalog")]
        [SerializeField] private DailyRewardCatalogSO _catalog;

        private readonly List<QuestItemBinding> _questItems = new();
        private readonly List<FreeRewardItemBinding> _freeRewardItems = new();
        private readonly List<UnityAction> _questButtonActions = new();
        private readonly List<UnityAction> _freeRewardButtonActions = new();

        private TMP_FontAsset _dailyGiftInitialFont;
        private Vector2 _dailyGiftImageInitialOffsetMin;
        private Vector2 _dailyGiftImageInitialOffsetMax;

        private Action _closeClicked;
        private Action<int> _questClicked;
        private Action _dailyGiftClicked;
        private Action<int> _freeRewardClicked;
        private Action _dayReset;
        private Coroutine _resetScrollCoroutine;
        private Button _dailyGiftButton;
        private TMP_Text _dailyGiftButtonText;
        private Image _dailyGiftRewardIcon;
        private Sprite _dailyGiftAvailableButtonSprite;
        private float _dailyGiftAvailablePixelsPerUnitMultiplier;
        private TMP_Text _resetTimerText;
        private GameObject _questsNotificationBadge;
        private TMP_Text _questsNotificationCount;
        private GameObject _freeNotificationBadge;
        private TMP_Text _freeNotificationCount;
        private RectTransform _progressHandle;
        private Image _progressHandleImage;
        private Image _progressLineImage;
        private Image _progressFillImage;
        private Slider _progressSlider;
        private RectTransform[] _progressSteps;
        private Sprite _progressIdleSprite;
        private Tween _progressSliderTween;
        private Sprite _availableFreeButtonSprite;
        private Sprite _lockedFreeButtonSprite;
        private Sprite _adButtonIconSprite;
        private Sprite _lockButtonIconSprite;
        private DateTimeOffset _resetAtUtc;
        private DateTimeOffset _dailyGiftAvailableAtUtc;
        private bool _dayResetRaised;
        private bool _giftCooldownFinishedRaised;

        private void Awake()
        {
            _popupAnimController ??= GetComponent<PopupAnimController>();
            ResolveContentBindings();

            _closeButton?.onClick.AddListener(OnCloseButtonClicked);
            _questsTabButton?.onClick.AddListener(OnQuestsTabButtonClicked);
            _freeCoinTabButton?.onClick.AddListener(OnFreeCoinTabButtonClicked);
            _dailyGiftButton?.onClick.AddListener(OnDailyGiftButtonClicked);
            AddItemListeners();
        }

        private void Update()
        {
            if (!IsOpened)
            {
                return;
            }

            UpdateDailyGiftCooldown();

            if (_resetTimerText == null)
            {
                return;
            }

            TimeSpan remaining = _resetAtUtc - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _resetTimerText.text = "00h 00m";
                if (!_dayResetRaised)
                {
                    _dayResetRaised = true;
                    _dayReset?.Invoke();
                }
                return;
            }

            int totalHours = Mathf.Max(0, (int)remaining.TotalHours);
            _resetTimerText.text = $"{totalHours:00}h {remaining.Minutes:00}m";
        }

        private void OnDisable()
        {
            StopScrollReset();
            StopProgressSliderTween();
        }

        private void OnDestroy()
        {
            StopScrollReset();
            StopProgressSliderTween();
            _closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
            _questsTabButton?.onClick.RemoveListener(OnQuestsTabButtonClicked);
            _freeCoinTabButton?.onClick.RemoveListener(OnFreeCoinTabButtonClicked);
            _dailyGiftButton?.onClick.RemoveListener(OnDailyGiftButtonClicked);

            for (int i = 0; i < _questItems.Count; i++)
            {
                _questItems[i].Button?.onClick.RemoveListener(_questButtonActions[i]);
            }

            for (int i = 0; i < _freeRewardItems.Count; i++)
            {
                _freeRewardItems[i].Button?.onClick.RemoveListener(
                    _freeRewardButtonActions[i]);
            }
        }

        public void SetActions(DailyRewardPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _questClicked = actions.QuestClicked;
            _dailyGiftClicked = actions.DailyGiftClicked;
            _freeRewardClicked = actions.FreeRewardClicked;
            _dayReset = actions.DayReset;
        }

        public void Bind(DailyRewardStatus status)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            _resetAtUtc = status.ResetAtUtc;
            _dayResetRaised = false;

            int questCount = Math.Min(_questItems.Count, status.Quests.Count);
            for (int i = 0; i < questCount; i++)
            {
                BindQuest(_questItems[i], status.Quests[i], i);
            }

            BindDailyGift(status);
            BindFreeRewards(status.AdRewardsClaimed, status.FinalBonusClaimed);
            BindNotificationBadge(
                _questsNotificationBadge,
                _questsNotificationCount,
                status.ClaimableQuestCount);
            BindNotificationBadge(
                _freeNotificationBadge,
                _freeNotificationCount,
                status.ClaimableFreeRewardCount);
            UpdateProgressVisual(
                status.AdRewardsClaimed,
                status.FinalBonusClaimed);
        }

        public DailyRewardCatalogSO Catalog => _catalog;

        public Sprite GetQuestRewardIcon(int index)
        {
            Sprite icon = index >= 0 && index < _questItems.Count
                ? _questItems[index].RewardIcon?.sprite
                : null;
            if (icon == null && _catalog != null)
            {
                icon = _catalog.CoinIcon;
            }

            return icon;
        }

        public Sprite GetDailyGiftRewardIcon()
        {
            return _dailyGiftRewardIcon?.sprite ?? _catalog?.DailyGiftIcon;
        }

        public Sprite GetFreeRewardIcon(int index)
        {
            Sprite icon = index >= 0 && index < _freeRewardItems.Count
                ? _freeRewardItems[index].RewardIcon?.sprite
                : null;
            if (icon == null && _catalog != null)
            {
                icon = _catalog.GetFreeRewardIcon(index);
            }

            return icon;
        }

        public override void Show()
        {
            base.Show();
            ShowQuests();
            _popupAnimController?.Open();
            RestartQuestScrollReset();
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy && _popupAnimController != null)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _questClicked = null;
            _dailyGiftClicked = null;
            _freeRewardClicked = null;
            _dayReset = null;
            base.Dispose();
        }

        public void ShowQuests()
        {
            _questContent?.SetActive(true);
            _freeCoinContent?.SetActive(false);
            SetTabVisual(_questsTabImage, _tabOnSprite, flipX: true);
            SetTabVisual(_freeCoinTabImage, _tabOffSprite, flipX: true);
        }

        public void ShowFreeCoin()
        {
            _questContent?.SetActive(false);
            _freeCoinContent?.SetActive(true);
            SetTabVisual(_questsTabImage, _tabOffSprite, flipX: false);
            SetTabVisual(_freeCoinTabImage, _tabOnSprite, flipX: false);
        }

        private void ResolveContentBindings()
        {
            Transform questRoot = _questScrollRect?.content;
            if (questRoot != null)
            {
                for (int i = 0; i < questRoot.childCount; i++)
                {
                    Transform itemRoot = questRoot.GetChild(i);
                    _questItems.Add(new QuestItemBinding(
                        itemRoot.GetComponentInChildren<Button>(true),
                        FindComponent<TMP_Text>(itemRoot, "QuestTitleText"),
                        FindComponent<TMP_Text>(itemRoot, "ProgressText"),
                        FindComponent<TMP_Text>(itemRoot, "RewardAmountText"),
                        FindComponent<Image>(itemRoot, "CoinIconImage")));
                }
            }

            Transform dailyCard = FindDescendant(
                _freeCoinContent?.transform,
                "DailyClaimCard");
            _dailyGiftButton = dailyCard?.GetComponentInChildren<Button>(true);
            _dailyGiftButtonText = FindButtonText(_dailyGiftButton);
            _dailyGiftRewardIcon = FindComponent<Image>(
                dailyCard,
                "RewardIconImage");

            Transform rewardList = FindDescendant(
                _freeCoinContent?.transform,
                "RewardListRoot");
            if (rewardList != null)
            {
                for (int i = 0; i < rewardList.childCount; i++)
                {
                    Transform itemRoot = rewardList.GetChild(i);
                    Button button = itemRoot.GetComponentInChildren<Button>(true);
                    _freeRewardItems.Add(new FreeRewardItemBinding(
                        button,
                        FindButtonText(button),
                        FindComponent<Image>(button?.transform, "Icon"),
                        FindComponent<Image>(itemRoot, "RewardIconImage")));
                }
            }

            CacheFreeRewardSprites();

            Transform timerRoot = FindDescendant(
                _freeCoinContent?.transform,
                "ResetTimerRoot");
            _resetTimerText = timerRoot?.GetComponentInChildren<TMP_Text>(true);

            ResolveNotificationBindings(
                _questsTabButton,
                out _questsNotificationBadge,
                out _questsNotificationCount);
            ResolveNotificationBindings(
                _freeCoinTabButton,
                out _freeNotificationBadge,
                out _freeNotificationCount);

            _progressHandle = FindDescendant(
                _freeCoinContent?.transform,
                "ProgressHandle") as RectTransform;
            _progressHandleImage = _progressHandle?.GetComponent<Image>();
            _progressLineImage = FindComponent<Image>(
                _freeCoinContent?.transform,
                "ProgressLineImage");
            Transform progressBarRoot = FindDescendant(
                _freeCoinContent?.transform,
                "ProgressBarRoot");
            _progressSlider = progressBarRoot?.GetComponent<Slider>();
            _progressFillImage = FindComponent<Image>(
                progressBarRoot,
                "ProgressFilledImage");
            Transform stepsRoot = FindDescendant(
                _freeCoinContent?.transform,
                "ProgressStepsRoot");
            if (stepsRoot != null)
            {
                _progressSteps = new RectTransform[stepsRoot.childCount];
                for (int i = 0; i < stepsRoot.childCount; i++)
                {
                    _progressSteps[i] = stepsRoot.GetChild(i) as RectTransform;
                }

                _progressIdleSprite = _progressSteps.Length > 0
                    ? _progressSteps[0]?.GetComponent<Image>()?.sprite
                    : null;
            }

            ConfigureProgressSlider();
        }

        private void CacheFreeRewardSprites()
        {
            Image dailyGiftButtonImage =
                _dailyGiftButton?.targetGraphic as Image;
            _dailyGiftAvailableButtonSprite = dailyGiftButtonImage?.sprite;
            _dailyGiftAvailablePixelsPerUnitMultiplier =
                dailyGiftButtonImage?.pixelsPerUnitMultiplier ??
                AvailableButtonPixelsPerUnitMultiplier;

            if (dailyGiftButtonImage != null)
            {
                _dailyGiftImageInitialOffsetMin = dailyGiftButtonImage.rectTransform.offsetMin;
                _dailyGiftImageInitialOffsetMax = dailyGiftButtonImage.rectTransform.offsetMax;
            }

            if (_dailyGiftButtonText != null)
            {
                _dailyGiftInitialFont = _dailyGiftButtonText.font;
            }

            if (_freeRewardItems.Count > 0)
            {
                _availableFreeButtonSprite =
                    (_freeRewardItems[0].Button?.targetGraphic as Image)?.sprite;
                _adButtonIconSprite = _freeRewardItems[0].Icon?.sprite;
            }

            if (_freeRewardItems.Count > 1)
            {
                _lockedFreeButtonSprite =
                    (_freeRewardItems[1].Button?.targetGraphic as Image)?.sprite;
                _lockButtonIconSprite = _freeRewardItems[1].Icon?.sprite;
            }
        }

        private void AddItemListeners()
        {
            for (int i = 0; i < _questItems.Count; i++)
            {
                int index = i;
                UnityAction action = () => _questClicked?.Invoke(index);
                _questButtonActions.Add(action);
                _questItems[i].Button?.onClick.AddListener(action);
            }

            for (int i = 0; i < _freeRewardItems.Count; i++)
            {
                int index = i;
                UnityAction action = () => _freeRewardClicked?.Invoke(index);
                _freeRewardButtonActions.Add(action);
                _freeRewardItems[i].Button?.onClick.AddListener(action);
            }
        }

        private void BindQuest(
            QuestItemBinding binding,
            DailyQuestStatus status,
            int index)
        {
            if (binding.TitleText != null)
            {
                binding.TitleText.text = _catalog != null
                    ? _catalog.GetQuestTitle(status.Type)
                    : (index < QuestTitles.Length ? QuestTitles[index] : status.Type.ToString());
            }

            if (binding.RewardIcon != null && binding.RewardIcon.sprite == null && _catalog != null)
            {
                binding.RewardIcon.sprite = _catalog.GetQuestIcon(status.Type);
            }

            SetText(binding.ProgressText, $"{status.Progress}/{status.Target}");
            SetText(binding.RewardText, status.CoinReward.ToString());

            if (binding.Button == null)
            {
                return;
            }

            TMP_Text buttonText = FindButtonText(binding.Button);
            if (status.IsClaimed)
            {
                binding.Button.interactable = false;
                SetText(buttonText, "Claimed");
            }
            else if (status.IsCompleted)
            {
                binding.Button.interactable = true;
                SetText(buttonText, "Claim");
            }
            else
            {
                binding.Button.interactable = true;
                SetText(buttonText, "Go");
            }
        }

        private void BindDailyGift(DailyRewardStatus status)
        {
            _dailyGiftAvailableAtUtc = status.DailyGiftAvailableAtUtc;
            _giftCooldownFinishedRaised = status.CanClaimDailyGift;
            UpdateDailyGiftVisual(status.NowUtc);
        }

        private void UpdateDailyGiftCooldown()
        {
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            UpdateDailyGiftVisual(nowUtc);

            if (_dailyGiftAvailableAtUtc > nowUtc ||
                _giftCooldownFinishedRaised)
            {
                return;
            }

            _giftCooldownFinishedRaised = true;
            _dayReset?.Invoke();
        }

        private void UpdateDailyGiftVisual(DateTimeOffset nowUtc)
        {
            if (_dailyGiftButton == null)
            {
                return;
            }

            TimeSpan remaining = _dailyGiftAvailableAtUtc - nowUtc;
            bool canClaim = remaining <= TimeSpan.Zero;
            _dailyGiftButton.interactable = canClaim;

            Image buttonImage = _dailyGiftButton.targetGraphic as Image;
            if (buttonImage != null)
            {
                Sprite targetSprite = canClaim
                    ? (_dailyGiftClaimSprite != null ? _dailyGiftClaimSprite : _dailyGiftAvailableButtonSprite)
                    : (_dailyGiftCooldownSprite != null ? _dailyGiftCooldownSprite : _lockedFreeButtonSprite);

                if (targetSprite != null)
                {
                    buttonImage.sprite = targetSprite;
                }
                buttonImage.type = Image.Type.Sliced;
                buttonImage.pixelsPerUnitMultiplier = canClaim
                    ? _dailyGiftClaimPixelsPerUnitMultiplier
                    : _dailyGiftCooldownPixelsPerUnitMultiplier;

                RectTransform imageRect = buttonImage.rectTransform;
                if (canClaim)
                {
                    imageRect.offsetMin = _dailyGiftImageInitialOffsetMin;
                    imageRect.offsetMax = _dailyGiftImageInitialOffsetMax;
                }
                else
                {
                    imageRect.offsetMin = new Vector2(
                        _dailyGiftImageInitialOffsetMin.x,
                        _dailyGiftCooldownImageBottom);
                    imageRect.offsetMax = new Vector2(
                        _dailyGiftImageInitialOffsetMax.x,
                        -_dailyGiftCooldownImageTop);
                }
            }

            if (_dailyGiftButtonText != null)
            {
                if (canClaim)
                {
                    if (_dailyGiftClaimFont != null)
                    {
                        _dailyGiftButtonText.font = _dailyGiftClaimFont;
                    }
                    else if (_dailyGiftInitialFont != null)
                    {
                        _dailyGiftButtonText.font = _dailyGiftInitialFont;
                    }

                    SetText(_dailyGiftButtonText, "Claim");
                    return;
                }

                if (_dailyGiftCooldownFont != null)
                {
                    _dailyGiftButtonText.font = _dailyGiftCooldownFont;
                }

                int totalSeconds = Mathf.Max(
                    0,
                    Mathf.CeilToInt((float)remaining.TotalSeconds));
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                SetText(_dailyGiftButtonText, $"{minutes:00}:{seconds:00}");
            }
        }

        private void BindFreeRewards(int adRewardsClaimed, bool finalBonusClaimed)
        {
            for (int i = 0; i < _freeRewardItems.Count; i++)
            {
                FreeRewardItemBinding item = _freeRewardItems[i];
                bool isClaimed = i < DailyRewardService.AdRewardCount
                    ? i < adRewardsClaimed
                    : finalBonusClaimed;
                bool isAvailable = i < DailyRewardService.AdRewardCount
                    ? i == adRewardsClaimed
                    : adRewardsClaimed == DailyRewardService.AdRewardCount &&
                      !finalBonusClaimed;

                if (item.Button != null)
                {
                    item.Button.interactable = isAvailable;
                    Image targetImage = item.Button.targetGraphic as Image;
                    if (targetImage != null)
                    {
                        targetImage.enabled = !isClaimed;
                        targetImage.sprite = isAvailable
                            ? _availableFreeButtonSprite
                            : _lockedFreeButtonSprite;
                        targetImage.type = Image.Type.Sliced;
                        targetImage.pixelsPerUnitMultiplier = isAvailable
                            ? AvailableButtonPixelsPerUnitMultiplier
                            : LockedButtonPixelsPerUnitMultiplier;
                    }
                }

                if (item.Icon != null)
                {
                    item.Icon.gameObject.SetActive(!isClaimed);
                    item.Icon.sprite = isAvailable
                        ? _adButtonIconSprite
                        : _lockButtonIconSprite;
                }

                if (item.ButtonText != null)
                {
                    item.ButtonText.color = isClaimed
                        ? _claimedTextColor
                        : item.DefaultTextColor;
                    item.ButtonText.font = isClaimed && _claimedFont != null
                        ? _claimedFont
                        : item.DefaultFont;
                }

                item.SetClaimedTextLayout(isClaimed);

                SetText(
                    item.ButtonText,
                    isClaimed ? "Claimed" : "Free");
            }
        }

        private void UpdateProgressVisual(
            int adRewardsClaimed,
            bool finalBonusClaimed)
        {
            if (_progressHandleImage == null ||
                _progressSteps == null ||
                _progressSteps.Length == 0)
            {
                return;
            }

            int currentSlot = Mathf.Clamp(
                adRewardsClaimed, 0, DailyRewardService.AdRewardCount);
            int slotCount = Mathf.Min(
                _progressSteps.Length + 1,
                DailyRewardService.AdRewardCount + 1);
            for (int slot = 0; slot < slotCount; slot++)
            {
                Image slotImage = slot == 0
                    ? _progressHandleImage
                    : _progressSteps[slot - 1]?.GetComponent<Image>();
                if (slotImage == null)
                {
                    continue;
                }

                bool isCompleted = slot < adRewardsClaimed ||
                    (slot == DailyRewardService.AdRewardCount &&
                     finalBonusClaimed);
                bool isCurrent = !finalBonusClaimed && slot == currentSlot;
                Sprite targetSprite = isCompleted
                    ? _progressCheckedSprite
                    : isCurrent
                        ? _progressCurrentSprite
                        : _progressIdleSprite;
                slotImage.sprite = targetSprite;
                slotImage.type = Image.Type.Simple;
                if (targetSprite != null)
                {
                    slotImage.SetNativeSize();
                }

                Vector2 anchoredPosition = slotImage.rectTransform.anchoredPosition;
                anchoredPosition.x = targetSprite == _progressCurrentSprite
                    ? ProgressCurrentPositionX
                    : ProgressDefaultPositionX;
                slotImage.rectTransform.anchoredPosition = anchoredPosition;

                slotImage.color = Color.white;
            }

            if (_progressSlider != null && _progressFillImage != null)
            {
                float targetValue = Mathf.Clamp(
                    adRewardsClaimed,
                    0,
                    DailyRewardService.AdRewardCount);
                _progressFillImage.gameObject.SetActive(targetValue > 0f);

                StopProgressSliderTween();
                if (IsOpened && targetValue > _progressSlider.value)
                {
                    _progressSliderTween = Tween.Custom(
                        this,
                        _progressSlider.value,
                        targetValue,
                        0.4f,
                        (view, value) => view.SetProgressSliderValue(value),
                        Ease.OutCubic,
                        useUnscaledTime: true);
                }
                else
                {
                    SetProgressSliderValue(targetValue);
                }
            }

            _progressHandle.gameObject.SetActive(true);
        }

        private void ConfigureProgressSlider()
        {
            if (_progressHandleImage != null)
            {
                if (_progressCurrentSprite != null)
                {
                    _progressHandleImage.sprite = _progressCurrentSprite;
                    _progressHandleImage.SetNativeSize();
                }

                _progressHandleImage.type = Image.Type.Simple;

                Vector2 handlePosition = _progressHandleImage.rectTransform.anchoredPosition;
                handlePosition.x = _progressHandleImage.sprite == _progressCurrentSprite
                    ? ProgressCurrentPositionX
                    : ProgressDefaultPositionX;
                _progressHandleImage.rectTransform.anchoredPosition = handlePosition;
            }

            if (_progressSlider == null ||
                _progressFillImage == null ||
                _progressFilledSprite == null)
            {
                return;
            }

            _progressFillImage.sprite = _progressFilledSprite;
            _progressFillImage.type = Image.Type.Sliced;
            _progressFillImage.raycastTarget = false;
            _progressSlider.transition = Selectable.Transition.None;
            _progressSlider.interactable = false;
            _progressSlider.targetGraphic = null;
            _progressSlider.fillRect = _progressFillImage.rectTransform;
            _progressSlider.handleRect = null;
            _progressSlider.direction = Slider.Direction.TopToBottom;
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = DailyRewardService.AdRewardCount;
            _progressSlider.wholeNumbers = false;
            _progressSlider.SetValueWithoutNotify(0f);
        }

        private void SetProgressSliderValue(float value)
        {
            _progressSlider?.SetValueWithoutNotify(value);
        }

        private void StopProgressSliderTween()
        {
            if (_progressSliderTween.isAlive)
            {
                _progressSliderTween.Stop();
            }

            _progressSliderTween = default;
        }

        private static void ResolveNotificationBindings(
            Button tabButton,
            out GameObject badge,
            out TMP_Text countText)
        {
            Transform badgeTransform = FindDescendant(
                tabButton?.transform,
                "NotificationBadge");
            badge = badgeTransform?.gameObject;
            countText = FindComponent<TMP_Text>(badgeTransform, "CountText");
        }

        private static void BindNotificationBadge(
            GameObject badge,
            TMP_Text countText,
            int count)
        {
            badge?.SetActive(count > 0);
            SetText(countText, count.ToString());
        }

        private static TMP_Text FindButtonText(Button button)
        {
            return button?.GetComponentInChildren<TMP_Text>(true);
        }

        private static T FindComponent<T>(Transform root, string objectName)
            where T : Component
        {
            Transform found = FindDescendant(root, objectName);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.Trim() == objectName)
                {
                    return child;
                }

                Transform nested = FindDescendant(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetTabVisual(Image image, Sprite sprite, bool flipX)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            Vector3 scale = image.rectTransform.localScale;
            scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            image.rectTransform.localScale = scale;
        }

        private void OnCloseButtonClicked()
        {
            if (_closeClicked != null)
            {
                _closeClicked();
            }
            else
            {
                RequestHide();
            }
        }

        private void OnDailyGiftButtonClicked()
        {
            _dailyGiftClicked?.Invoke();
        }

        private void OnQuestsTabButtonClicked()
        {
            ShowQuests();
            RestartQuestScrollReset();
        }

        private void OnFreeCoinTabButtonClicked()
        {
            ShowFreeCoin();
        }

        private void RestartQuestScrollReset()
        {
            StopScrollReset();
            _resetScrollCoroutine = StartCoroutine(ResetQuestScrollPosition());
        }

        private void StopScrollReset()
        {
            if (_resetScrollCoroutine == null)
            {
                return;
            }

            StopCoroutine(_resetScrollCoroutine);
            _resetScrollCoroutine = null;
        }

        private IEnumerator ResetQuestScrollPosition()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (_questScrollRect == null)
            {
                _resetScrollCoroutine = null;
                yield break;
            }

            _questScrollRect.StopMovement();
            _questScrollRect.velocity = Vector2.zero;
            Canvas.ForceUpdateCanvases();

            if (_questScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_questScrollRect.content);
                Vector2 position = _questScrollRect.content.anchoredPosition;
                position.y = 0f;
                _questScrollRect.content.anchoredPosition = position;
            }

            Canvas.ForceUpdateCanvases();
            _questScrollRect.verticalNormalizedPosition = 1f;
            _questScrollRect.velocity = Vector2.zero;
            _resetScrollCoroutine = null;
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

        private sealed class QuestItemBinding
        {
            public QuestItemBinding(
                Button button,
                TMP_Text titleText,
                TMP_Text progressText,
                TMP_Text rewardText,
                Image rewardIcon)
            {
                Button = button;
                TitleText = titleText;
                ProgressText = progressText;
                RewardText = rewardText;
                RewardIcon = rewardIcon;
            }

            public Button Button { get; }
            public TMP_Text TitleText { get; }
            public TMP_Text ProgressText { get; }
            public TMP_Text RewardText { get; }
            public Image RewardIcon { get; }
        }

        private sealed class FreeRewardItemBinding
        {
            public FreeRewardItemBinding(
                Button button,
                TMP_Text buttonText,
                Image icon,
                Image rewardIcon)
            {
                Button = button;
                ButtonText = buttonText;
                Icon = icon;
                RewardIcon = rewardIcon;
                DefaultTextColor = buttonText != null
                    ? buttonText.color
                    : Color.white;
                DefaultFont = buttonText?.font;
                ButtonTextRect = buttonText?.rectTransform;
                DefaultButtonTextOffsetMin = ButtonTextRect != null
                    ? ButtonTextRect.offsetMin
                    : Vector2.zero;
            }

            public Button Button { get; }
            public TMP_Text ButtonText { get; }
            public Image Icon { get; }
            public Image RewardIcon { get; }
            public Color DefaultTextColor { get; }
            public TMP_FontAsset DefaultFont { get; }
            private RectTransform ButtonTextRect { get; }
            private Vector2 DefaultButtonTextOffsetMin { get; }

            public void SetClaimedTextLayout(bool isClaimed)
            {
                if (ButtonTextRect == null)
                {
                    return;
                }

                Vector2 offsetMin = ButtonTextRect.offsetMin;
                offsetMin.x = isClaimed
                    ? 0f
                    : DefaultButtonTextOffsetMin.x;
                ButtonTextRect.offsetMin = offsetMin;
            }
        }
    }
}
