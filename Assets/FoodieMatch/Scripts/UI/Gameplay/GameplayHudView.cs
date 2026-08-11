using System;
using System.Collections;
using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Gameplay.Booster;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        private const int BoosterButtonCount = 4;

        [Header("Motion")]
        [SerializeField] private PopupAnimController _animController;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private BoosterButtonView[] _boosterButtonViews;
        [SerializeField] private TMP_Text[] _boosterCountTexts;
        [SerializeField] private TMP_Text _levelLabelText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _comboProgressBarRoot;
        [SerializeField] private TMP_Text _comboMultiplierText;
        [SerializeField] private Image _comboBarFillImage;
        [SerializeField] private ComboBarAnimController _comboBarAnimController;

        [Header("Booster Unlock Reward")]
        [SerializeField] private BoosterUnlockRewardView _boosterUnlockRewardView;

        [Header("Combo Feedback")]
        [SerializeField] private RectTransform _comboFeedbackRoot;

        [Header("Tutorial")]
        [SerializeField] private GameObject _tutorialRoot;
        [SerializeField] private TutorialHandView _tutorialHandView;

        private ComboFeedbackViewPool _comboFeedbackPool;
        private Action _pauseClicked;
        private Action<int> _boosterUseClicked;
        private Action<int> _boosterAddClicked;
        private int _lastComboCount;
        private Coroutine _breakClearCoroutine;
        private Tween _comboCountdownTween;
        private TaskCompletionSource<bool> _motionCompletion;

        private void Awake()
        {
            BindButtons();
            ResetCombo();
            _boosterUnlockRewardView.Initialize();
        }

        private void OnDestroy()
        {
            StopComboCountdown();
            StopBreakClearCoroutine();
            UnbindButtons();
        }

        private void OnDisable()
        {
            CompleteMotion(false);
            HideTutorial();
            _boosterUnlockRewardView.StopAndHide();
            _comboFeedbackPool?.ReleaseAll();
        }

        public void Construct(
            ComboFeedbackViewPool comboFeedbackPool)
        {
            _comboFeedbackPool = comboFeedbackPool;
        }

        public Task OpenAsync()
        {
            return PlayMotionAsync(_animController.Open);
        }

        public Task CloseAsync()
        {
            return PlayMotionAsync(_animController.Close);
        }

        public void HideInstantly()
        {
            CompleteMotion(false);
            _animController.HideInstantly();
        }

        public void SetActions(GameplayHudViewActions actions)
        {
            _pauseClicked = actions.PauseClicked;
            _boosterUseClicked = actions.BoosterUseClicked;
            _boosterAddClicked = actions.BoosterAddClicked;
            SetupBoosterButtonViews();
        }

        public void SetLevelNumber(int levelNumber)
        {
            _levelLabelText.text = levelNumber.ToString();
        }

        public void SetPauseButtonVisible(bool visible)
        {
            _pauseButton.gameObject.SetActive(visible);
        }

        public void SetControlsInteractable(bool interactable)
        {
            _pauseButton.interactable = interactable;

            for (int i = 0; i < _boosterButtonViews.Length; i++)
            {
                _boosterButtonViews[i].SetInputEnabled(interactable);
            }
        }

        public void SetProgress(int servedCount, int totalCount)
        {
            _progressText.text = $"{servedCount}/{totalCount}";
        }

        public void SetCombo(int comboCount, float remainingSeconds)
        {
            bool isBreaking = comboCount <= 0 && _lastComboCount > 0;

            if (comboCount > 0)
            {
                StopBreakClearCoroutine();
                _comboMultiplierText.text = $"x{comboCount}";
                PlayComboCountdown(remainingSeconds);
                ResetComboMultiplierVisual();
            }
            else
            {
                StopComboCountdown();
                SetComboFill(0f);

                if (!isBreaking)
                {
                    ClearComboVisualImmediate();
                }
            }

            PlayComboAnimIfNeeded(comboCount);
        }

        public void ResetCombo()
        {
            StopComboCountdown();
            StopBreakClearCoroutine();
            _lastComboCount = 0;
            ClearComboVisualImmediate();
        }

        public void ShowComboFeedback(Vector3 worldPosition)
        {
            Camera worldCamera = Camera.main;

            if (worldCamera == null)
            {
                Debug.LogError("Main camera is missing for combo feedback.", this);
                return;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z <= 0f || !TryGetComboFeedbackPosition(screenPosition, out Vector2 localPosition))
            {
                return;
            }

            _comboFeedbackPool.Play(
                _comboFeedbackRoot,
                localPosition);
        }

        public void ShowTutorialHand(Vector2 screenPosition)
        {
            _tutorialHandView.ShowAt(screenPosition);
        }

        public void ShowTutorial()
        {
            _tutorialRoot.SetActive(true);
        }

        public Task<MotionResult> MoveTutorialHandAsync(Vector2 screenPosition)
        {
            return _tutorialHandView.MoveToAsync(screenPosition);
        }

        public void HideTutorialHand()
        {
            _tutorialHandView.Hide();
        }

        public void HideTutorial()
        {
            _tutorialRoot.SetActive(false);
        }

        private void PlayComboAnimIfNeeded(int comboCount)
        {
            if (comboCount == _lastComboCount)
            {
                return;
            }

            if (comboCount > _lastComboCount)
            {
                StopBreakClearCoroutine();

                if (_lastComboCount <= 0)
                {
                    _comboBarAnimController.PlayStart();
                }
                else
                {
                    _comboBarAnimController.PlayContinue();
                }
            }
            else if (comboCount <= 0 && _lastComboCount > 0)
            {
                _comboBarAnimController.PlayBreak();
                StopBreakClearCoroutine();
                _breakClearCoroutine = StartCoroutine(ClearComboVisualAfterBreak());
            }

            _lastComboCount = comboCount;
        }

        private IEnumerator ClearComboVisualAfterBreak()
        {
            float wait = _comboBarAnimController != null
                ? _comboBarAnimController.BreakDuration
                : 0.6f;

            yield return new WaitForSecondsRealtime(wait);

            if (_lastComboCount <= 0)
            {
                ClearComboVisualImmediate();
            }

            _breakClearCoroutine = null;
        }

        private void ClearComboVisualImmediate()
        {
            StopComboCountdown();
            _comboMultiplierText.text = string.Empty;
            SetComboFill(0f);
            ResetComboMultiplierVisual();
        }

        private void PlayComboCountdown(float remainingSeconds)
        {
            StopComboCountdown();

            if (_comboBarFillImage == null || !IsValidDuration(remainingSeconds))
            {
                SetComboFill(0f);
                return;
            }

            SetComboFill(1f);
            _comboCountdownTween = Tween.Custom(
                this,
                1f,
                0f,
                remainingSeconds,
                (view, fill) => view.SetComboFill(fill),
                Ease.Linear);
        }

        private void StopComboCountdown()
        {
            if (_comboCountdownTween.isAlive)
            {
                _comboCountdownTween.Stop();
            }

            _comboCountdownTween = default;
        }

        private void SetComboFill(float fill)
        {
            if (_comboBarFillImage != null)
            {
                _comboBarFillImage.fillAmount = Mathf.Clamp01(fill);
            }
        }

        private void ResetComboMultiplierVisual()
        {
            if (_comboMultiplierText == null)
            {
                return;
            }

            Transform textTransform = _comboMultiplierText.transform;
            textTransform.localScale = Vector3.one;

            Color color = _comboMultiplierText.color;
            color.a = 1f;
            _comboMultiplierText.color = color;
        }

        private void StopBreakClearCoroutine()
        {
            if (_breakClearCoroutine == null)
            {
                return;
            }

            StopCoroutine(_breakClearCoroutine);
            _breakClearCoroutine = null;
        }

        public void SetBoosterCount(int boosterIndex, int count)
        {
            if (boosterIndex < 0 ||
                boosterIndex >= _boosterCountTexts.Length)
            {
                return;
            }

            _boosterCountTexts[boosterIndex].text = count.ToString();

            if (boosterIndex < _boosterButtonViews.Length)
            {
                _boosterButtonViews[boosterIndex].SetCount(count);
            }
        }

        public void SetBoosterUnlocked(int boosterIndex, bool isUnlocked)
        {
            if (boosterIndex < 0 || boosterIndex >= _boosterButtonViews.Length)
            {
                return;
            }

            _boosterButtonViews[boosterIndex].SetUnlocked(isUnlocked);
        }

        public void SetBoosterLockedSprites(
            int boosterIndex,
            Sprite lockedButtonSprite,
            Sprite lockedIconSprite)
        {
            if (boosterIndex < 0 || boosterIndex >= _boosterButtonViews.Length)
            {
                return;
            }

            _boosterButtonViews[boosterIndex].SetLockedSprites(
                lockedButtonSprite,
                lockedIconSprite);
        }

        public void SetBoosterUnlockLevel(int boosterIndex, int unlockLevel)
        {
            if (boosterIndex < 0 || boosterIndex >= _boosterButtonViews.Length)
            {
                return;
            }

            _boosterButtonViews[boosterIndex].SetUnlockLevel(unlockLevel);
        }

        public void SetBoosterCounts(int[] counts)
        {
            int length = Mathf.Min(counts.Length, _boosterCountTexts.Length);

            for (int i = 0; i < length; i++)
            {
                _boosterCountTexts[i].text = counts[i].ToString();

                if (i < _boosterButtonViews.Length)
                {
                    _boosterButtonViews[i].SetCount(counts[i]);
                }
            }
        }

        public void SetBoosterUnlockedStates(bool[] unlockedStates)
        {
            int length = Mathf.Min(unlockedStates.Length, _boosterButtonViews.Length);

            for (int i = 0; i < length; i++)
            {
                _boosterButtonViews[i].SetUnlocked(unlockedStates[i]);
            }
        }

        public Task<MotionResult> PlayBoosterUnlockRewardAsync(
            int boosterIndex,
            Sprite icon,
            int amount)
        {
            return _boosterUnlockRewardView.PlayAsync(
                icon,
                amount,
                _boosterButtonViews[boosterIndex].RewardTarget);
        }

        public void StopBoosterUnlockReward()
        {
            _boosterUnlockRewardView.StopAndHide();
        }

        private void BindButtons()
        {
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        private void UnbindButtons()
        {
            _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        }

        private void SetupBoosterButtonViews()
        {
            for (int i = 0; i < _boosterButtonViews.Length && i < BoosterButtonCount; i++)
            {
                BoosterButtonView view = _boosterButtonViews[i];
                int index = i;
                view.SetActions(
                    useBoosterClicked: () => _boosterUseClicked?.Invoke(index),
                    addBoosterClicked: () => _boosterAddClicked?.Invoke(index));
            }
        }

        private void OnPauseButtonClicked()
        {
            _pauseClicked?.Invoke();
        }

        private static bool IsValidDuration(float duration)
        {
            return duration > 0f && !float.IsNaN(duration) && !float.IsInfinity(duration);
        }

        private Task PlayMotionAsync(Action<Action> playMotion)
        {
            CompleteMotion(false);

            TaskCompletionSource<bool> completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _motionCompletion = completion;
            playMotion(() => CompleteMotion(completion));
            return completion.Task;
        }

        private void CompleteMotion(
            TaskCompletionSource<bool> completion)
        {
            if (_motionCompletion == completion)
            {
                _motionCompletion = null;
            }

            completion.TrySetResult(true);
        }

        private void CompleteMotion(bool completed)
        {
            TaskCompletionSource<bool> completion = _motionCompletion;
            _motionCompletion = null;
            completion?.TrySetResult(completed);
        }

        private bool TryGetComboFeedbackPosition(Vector3 screenPosition, out Vector2 localPosition)
        {
            Canvas canvas = _comboFeedbackRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _comboFeedbackRoot,
                screenPosition,
                uiCamera,
                out localPosition);
        }
    }
}
