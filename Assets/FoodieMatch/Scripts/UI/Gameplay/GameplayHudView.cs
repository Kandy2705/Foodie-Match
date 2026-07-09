using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;

        private Action _pauseClicked;

        private void Awake()
        {
            EnsureButtonReferences();

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

        private void OnPauseButtonClicked()
        {
            _pauseClicked?.Invoke();
        }

        private void EnsureButtonReferences()
        {
            if (_pauseButton == null)
            {
                // Prefab currently names this SettingsButton, but gameplay uses it as Pause.
                _pauseButton = FindChildButton("SettingsButton");
            }

            if (_pauseButton == null)
            {
                _pauseButton = FindChildButton("PauseButton");
            }
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
