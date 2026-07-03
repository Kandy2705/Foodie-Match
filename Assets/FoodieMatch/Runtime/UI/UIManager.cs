using System;
using FoodieMatch.Runtime.Core.Application.Events;
using FoodieMatch.Runtime.Core.Infrastructure.Audio;
using FoodieMatch.Runtime.UI.Popup;
using FoodieMatch.Runtime.UI.Result;
using UnityEngine;
namespace FoodieMatch.Runtime.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string PopupShowSfxKey = "popup_show";

        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private GameObject _gameplayHud;
        private bool _hasConstructed;

        public void Construct(
            GameplayEvents gameplayEvents,
            IAudioService audioService)
        {
            if (_hasConstructed)
            {
                UnsubscribeEvents();
            }

            _audioService = audioService;

            _gameplayEvents = gameplayEvents;
            SubscribeEvents();

            _hasConstructed = true;

            if (_popupManager == null)
            {
                Debug.LogError("PopupManager is missing.");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        public void ShowHome()
        {
            Debug.Log("Show Home");
        }

        public void ShowGameplayHud()
        {
            if (_gameplayHud != null)
            {
                _gameplayHud.SetActive(true);
                return;
            }

            if (_gameplayHudPrefab == null || _hudRoot == null)
            {
                Debug.Log("Show Gameplay HUD");
                return;
            }

            _gameplayHud = Instantiate(_gameplayHudPrefab, _hudRoot);
            _gameplayHud.gameObject.name = _gameplayHudPrefab.gameObject.name;
        }
        public void HideGameplayHud()
        {
            if (_gameplayHud == null)
            {
                return;
            }

            _gameplayHud.SetActive(false);
        }

        public void ShowWinPopup(
            Action nextClicked,
            Action homeClicked)
        {
            Debug.Log("Show Win Popup");
            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void ShowLosePopup(
            int levelId,
            string reason,
            Action retryClicked,
            Action homeClicked)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show lose popup because PopupManager is missing.");
                return;
            }

            LosePopupData popupData = new LosePopupData(levelId, reason);
            LosePopupView popup = _popupManager.Show<LosePopupView>(popupData);

            if (popup == null)
            {
                return;
            }
            _audioService?.PlaySfx(PopupShowSfxKey);
            popup.SetActions(retryClicked, homeClicked);
        }

        public void HideAllPopups()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.HideAll();
        }

        private void SubscribeEvents()
        {
            if (_gameplayEvents == null)
            {
                return;
            }

            _gameplayEvents.LevelStarted += OnLevelStarted;
            _gameplayEvents.LevelProgressChanged += OnLevelProgressChanged;
            _gameplayEvents.LevelEnded += OnLevelEnded;
        }

        private void UnsubscribeEvents()
        {
            if (_gameplayEvents == null)
            {
                return;
            }

            _gameplayEvents.LevelStarted -= OnLevelStarted;
            _gameplayEvents.LevelProgressChanged -= OnLevelProgressChanged;
            _gameplayEvents.LevelEnded -= OnLevelEnded;
        }

        private void OnLevelStarted(LevelStartedEvent eventData)
        {
            Debug.Log($"Level Started: {eventData.LevelId}");
        }

        private void OnLevelProgressChanged(LevelProgressChangedEvent eventData)
        {
            Debug.Log($"Progress: {eventData.ServedCount}/{eventData.TotalCount}");
        }

        private void OnLevelEnded(LevelEndedEvent eventData)
        {
            Debug.Log($"Level Ended: {eventData.LevelId}, Win: {eventData.IsWin}, Reason: {eventData.Reason}");
        }
    }
}
