using System;
using FoodieMatch.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _levelLabelText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _comboMultiplierText;
        [SerializeField] private TMP_Text[] _boosterCountTexts;

        private Action _pauseClicked;

        private void Awake()
        {
            EnsureButtonReferences();
            EnsureTextReferences();

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            else
            {
                Debug.LogWarning($"{nameof(GameplayHudView)} on {name} has no pause button assigned.");
            }
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            }
        }

        public void SetActions(GameplayHudViewActions actions)
        {
            _pauseClicked = actions.PauseClicked;
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

        public void SetComboMultiplier(int multiplier)
        {
            EnsureTextReferences();
            UiTmpText.SetText(_comboMultiplierText, $"x{multiplier}");
        }

        public void SetComboMultiplier(string multiplierText)
        {
            EnsureTextReferences();
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
