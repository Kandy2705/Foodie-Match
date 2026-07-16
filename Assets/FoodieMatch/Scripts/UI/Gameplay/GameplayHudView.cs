using System;
using FoodieMatch.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        private const int BoosterButtonCount = 4;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button[] _boosterButtons;
        [SerializeField] private TMP_Text _levelLabelText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _comboProgressBarRoot;
        [SerializeField] private TMP_Text _comboMultiplierText;
        [SerializeField] private Image _comboBarFillImage;
        [SerializeField] private TMP_Text[] _boosterCountTexts;

        private Action _pauseClicked;
        private Action<int> _boosterClicked;
        private readonly UnityAction[] _boosterButtonHandlers = new UnityAction[BoosterButtonCount];

        private void Awake()
        {
            EnsureButtonReferences();
            EnsureTextReferences();
            EnsureComboReferences();
            BindButtons();
            SetCombo(0, 0f);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void SetActions(GameplayHudViewActions actions)
        {
            _pauseClicked = actions.PauseClicked;
            _boosterClicked = actions.BoosterClicked;
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

            if (comboCount <= 0)
            {
                UiTmpText.SetText(_comboMultiplierText, string.Empty);

                if (_comboBarFillImage != null)
                {
                    _comboBarFillImage.fillAmount = 0f;
                }

                return;
            }

            UiTmpText.SetText(_comboMultiplierText, $"x{comboCount}");

            if (_comboBarFillImage != null)
            {
                _comboBarFillImage.fillAmount = Mathf.Clamp01(fillNormalized);
            }
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

            if (_boosterButtons == null)
            {
                return;
            }

            for (int i = 0; i < _boosterButtons.Length && i < BoosterButtonCount; i++)
            {
                Button button = _boosterButtons[i];

                if (button == null)
                {
                    continue;
                }

                int boosterIndex = i;
                UnityAction handler = () => OnBoosterButtonClicked(boosterIndex);
                _boosterButtonHandlers[i] = handler;
                button.onClick.AddListener(handler);
            }
        }

        private void UnbindButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            }

            if (_boosterButtons == null)
            {
                return;
            }

            for (int i = 0; i < _boosterButtons.Length && i < BoosterButtonCount; i++)
            {
                Button button = _boosterButtons[i];
                UnityAction handler = _boosterButtonHandlers[i];

                if (button == null || handler == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(handler);
                _boosterButtonHandlers[i] = null;
            }
        }

        private void OnPauseButtonClicked()
        {
            _pauseClicked?.Invoke();
        }

        private void OnBoosterButtonClicked(int boosterIndex)
        {
            _boosterClicked?.Invoke(boosterIndex);
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

            if (_boosterButtons == null || _boosterButtons.Length == 0)
            {
                _boosterButtons = FindBoosterButtons();
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
            if (_comboProgressBarRoot == null)
            {
                Transform comboRoot = FindChildTransform("ComboProgressBarRoot");

                if (comboRoot != null)
                {
                    _comboProgressBarRoot = comboRoot.gameObject;
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
                Transform fillTransform = FindChildTransform("BarFillImage");

                if (fillTransform != null)
                {
                    _comboBarFillImage = fillTransform.GetComponent<Image>();
                }
            }
        }

        private Button[] FindBoosterButtons()
        {
            Button[] buttons = new Button[BoosterButtonCount];

            for (int i = 0; i < BoosterButtonCount; i++)
            {
                string objectName = $"BoosterButton_0{i + 1}";
                buttons[i] = FindChildButton(objectName);
            }

            return buttons;
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
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

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
