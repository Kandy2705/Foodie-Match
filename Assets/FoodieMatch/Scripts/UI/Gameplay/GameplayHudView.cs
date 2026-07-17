using System;
using System.Collections;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Gameplay.Booster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        private const int BoosterButtonCount = 4;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private BoosterButtonView[] _boosterButtonViews;
        [SerializeField] private TMP_Text _levelLabelText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _comboProgressBarRoot;
        [SerializeField] private TMP_Text _comboMultiplierText;
        [SerializeField] private Image _comboBarFillImage;
        [SerializeField] private ComboBarAnimController _comboBarAnimController;
        [SerializeField] private TMP_Text[] _boosterCountTexts;

        private Action _pauseClicked;
        private Action<int> _boosterUseClicked;
        private Action<int> _boosterAddClicked;
        private int _lastComboCount;
        private Coroutine _breakClearCoroutine;

        private void Awake()
        {
            EnsureButtonReferences();
            EnsureTextReferences();
            EnsureComboReferences();
            BindButtons();
            _lastComboCount = 0;
            SetCombo(0, 0f);
        }

        private void OnDestroy()
        {
            StopBreakClearCoroutine();
            UnbindButtons();
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
            EnsureTextReferences();
            UiTmpText.SetText(_levelLabelText, levelNumber.ToString());
        }

        public void SetProgress(int servedCount, int totalCount)
        {
            EnsureTextReferences();
            UiTmpText.SetText(_progressText, $"{servedCount}/{totalCount}");
        }

        public void SetCombo(int comboCount, float fillNormalized)
        {
            EnsureComboReferences();

            bool isBreaking = comboCount <= 0 && _lastComboCount > 0;

            if (comboCount > 0)
            {
                StopBreakClearCoroutine();
                UiTmpText.SetText(_comboMultiplierText, $"x{comboCount}");

                if (_comboBarFillImage != null)
                {
                    _comboBarFillImage.fillAmount = Mathf.Clamp01(fillNormalized);
                }

                ResetComboMultiplierVisual();
            }
            else if (isBreaking)
            {
                if (_comboBarFillImage != null)
                {
                    _comboBarFillImage.fillAmount = 0f;
                }
            }
            else
            {
                ClearComboVisualImmediate();
            }

            PlayComboAnimIfNeeded(comboCount);
        }

        private void PlayComboAnimIfNeeded(int comboCount)
        {
            if (comboCount == _lastComboCount)
            {
                return;
            }

            if (_comboBarAnimController != null)
            {
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
            }
            else if (comboCount <= 0)
            {
                ClearComboVisualImmediate();
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
            UiTmpText.SetText(_comboMultiplierText, string.Empty);

            if (_comboBarFillImage != null)
            {
                _comboBarFillImage.fillAmount = 0f;
            }

            ResetComboMultiplierVisual();
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

        public void SetComboMultiplier(int multiplier)
        {
            SetCombo(multiplier, multiplier > 0 ? 1f : 0f);
        }

        public void SetComboMultiplier(string multiplierText)
        {
            EnsureComboReferences();
            UiTmpText.SetText(_comboMultiplierText, multiplierText);
        }

        public void SetBoosterCount(int boosterIndex, int count)
        {
            EnsureTextReferences();

            if (_boosterCountTexts == null ||
                boosterIndex < 0 ||
                boosterIndex >= _boosterCountTexts.Length)
            {
                return;
            }

            UiTmpText.SetText(_boosterCountTexts[boosterIndex], count.ToString());

            if (_boosterButtonViews != null &&
                boosterIndex >= 0 &&
                boosterIndex < _boosterButtonViews.Length &&
                _boosterButtonViews[boosterIndex] != null)
            {
                _boosterButtonViews[boosterIndex].SetCount(count);
            }
        }

        public void SetBoosterUnlocked(int boosterIndex, bool isUnlocked)
        {
            if (_boosterButtonViews == null ||
                boosterIndex < 0 ||
                boosterIndex >= _boosterButtonViews.Length ||
                _boosterButtonViews[boosterIndex] == null)
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
            if (_boosterButtonViews == null ||
                boosterIndex < 0 ||
                boosterIndex >= _boosterButtonViews.Length ||
                _boosterButtonViews[boosterIndex] == null)
            {
                return;
            }

            _boosterButtonViews[boosterIndex].SetLockedSprites(
                lockedButtonSprite,
                lockedIconSprite);
        }

        public void SetBoosterUnlockLevel(int boosterIndex, int unlockLevel)
        {
            if (_boosterButtonViews == null ||
                boosterIndex < 0 ||
                boosterIndex >= _boosterButtonViews.Length ||
                _boosterButtonViews[boosterIndex] == null)
            {
                return;
            }

            _boosterButtonViews[boosterIndex].SetUnlockLevel(unlockLevel);
        }

        public void SetBoosterCounts(int[] counts)
        {
            EnsureTextReferences();

            if (counts == null || _boosterCountTexts == null)
            {
                return;
            }

            int length = Mathf.Min(counts.Length, _boosterCountTexts.Length);

            for (int i = 0; i < length; i++)
            {
                UiTmpText.SetText(_boosterCountTexts[i], counts[i].ToString());

                if (_boosterButtonViews != null && i < _boosterButtonViews.Length && _boosterButtonViews[i] != null)
                {
                    _boosterButtonViews[i].SetCount(counts[i]);
                }
            }
        }

        public void SetBoosterUnlockedStates(bool[] unlockedStates)
        {
            if (unlockedStates == null || _boosterButtonViews == null)
            {
                return;
            }

            int length = Mathf.Min(unlockedStates.Length, _boosterButtonViews.Length);

            for (int i = 0; i < length; i++)
            {
                if (_boosterButtonViews[i] != null)
                {
                    _boosterButtonViews[i].SetUnlocked(unlockedStates[i]);
                }
            }
        }

        private void BindButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            else
            {
                Debug.LogWarning($"{nameof(GameplayHudView)} on {name} has no pause button assigned.");
            }
        }

        private void UnbindButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            }
        }

        private void SetupBoosterButtonViews()
        {
            if (_boosterButtonViews == null)
            {
                return;
            }

            for (int i = 0; i < _boosterButtonViews.Length && i < BoosterButtonCount; i++)
            {
                BoosterButtonView view = _boosterButtonViews[i];

                if (view == null)
                {
                    continue;
                }

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

        private void EnsureButtonReferences()
        {
            if (_pauseButton == null)
            {
                _pauseButton = FindChildButton("SettingsButton");
            }

            if (_pauseButton == null)
            {
                _pauseButton = FindChildButton("PauseButton");
            }

            if (_boosterButtonViews == null || _boosterButtonViews.Length == 0)
            {
                _boosterButtonViews = FindBoosterButtonViews();
            }
        }

        private void EnsureTextReferences()
        {
            if (_levelLabelText == null)
            {
                _levelLabelText = UiTmpText.FindChild(transform, "Label");
            }

            if (_progressText == null)
            {
                _progressText = UiTmpText.FindChild(transform, "ProgressText");
            }

            if (_comboMultiplierText == null)
            {
                _comboMultiplierText = UiTmpText.FindChild(transform, "ComboMultiplierText");
            }

            if (_boosterCountTexts == null || _boosterCountTexts.Length == 0)
            {
                _boosterCountTexts = FindBoosterCountTexts();
            }
        }

        private void EnsureComboReferences()
        {
            Transform comboRootTransform = null;

            if (_comboProgressBarRoot != null)
            {
                if (_comboProgressBarRoot.name != "ComboProgressBarRoot")
                {
                    Transform nested = FindChildTransform(
                        _comboProgressBarRoot.transform,
                        "ComboProgressBarRoot");

                    if (nested != null)
                    {
                        _comboProgressBarRoot = nested.gameObject;
                    }
                }

                comboRootTransform = _comboProgressBarRoot.transform;
            }

            if (_comboProgressBarRoot == null)
            {
                comboRootTransform = FindChildTransform(transform, "ComboProgressBarRoot");

                if (comboRootTransform != null)
                {
                    _comboProgressBarRoot = comboRootTransform.gameObject;
                }
            }

            if (_comboMultiplierText == null)
            {
                Transform searchRoot = _comboProgressBarRoot != null
                    ? _comboProgressBarRoot.transform
                    : transform;
                _comboMultiplierText = UiTmpText.FindChild(searchRoot, "ComboMultiplierText");
            }

            if (_comboBarFillImage == null)
            {
                Transform fillTransform = FindChildTransform(transform, "BarFillImage");

                if (fillTransform != null)
                {
                    _comboBarFillImage = fillTransform.GetComponent<Image>();
                }
            }

            if (_comboBarAnimController == null)
            {
                Transform comboButton = FindChildTransform(transform, "ComboProgressButton");

                if (comboButton != null)
                {
                    _comboBarAnimController =
                        comboButton.GetComponent<ComboBarAnimController>();
                }

                if (_comboBarAnimController == null && _comboProgressBarRoot != null)
                {
                    _comboBarAnimController =
                        _comboProgressBarRoot.GetComponentInParent<ComboBarAnimController>();

                    if (_comboBarAnimController == null)
                    {
                        _comboBarAnimController =
                            _comboProgressBarRoot.GetComponent<ComboBarAnimController>();
                    }
                }

                if (_comboBarAnimController == null)
                {
                    _comboBarAnimController =
                        GetComponentInChildren<ComboBarAnimController>(true);
                }
            }
        }

        private BoosterButtonView[] FindBoosterButtonViews()
        {
            BoosterButtonView[] views = new BoosterButtonView[BoosterButtonCount];

            for (int i = 0; i < BoosterButtonCount; i++)
            {
                string objectName = $"BoosterButton_0{i + 1}";

                Transform child = FindChildTransform(objectName);
                views[i] = child != null
                    ? child.GetComponent<BoosterButtonView>()
                    : null;
            }

            return views;
        }

        private TMP_Text[] FindBoosterCountTexts()
        {
            Transform boosterBar = FindChildTransform("BoosterBarRoot");

            if (boosterBar == null)
            {
                boosterBar = transform;
            }

            System.Collections.Generic.List<TMP_Text> countTexts = new();
            TMP_Text[] texts = boosterBar.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text != null && text.gameObject.name == "CountText")
                {
                    countTexts.Add(text);
                }
            }

            return countTexts.ToArray();
        }

        private Transform FindChildTransform(string objectName)
        {
            return FindChildTransform(transform, objectName);
        }

        private static Transform FindChildTransform(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];

                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private Button FindChildButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button != null && button.gameObject.name == objectName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
