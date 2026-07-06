using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string PopupShowSfxKey = "popup_show";

        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;

        [Header("Home")]
        [SerializeField] private Transform _mainMenuRoot;
        [SerializeField] private HomeView _homeView;

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private GameObject _gameplayHud;
        private bool _hasConstructed;

        public event Action PlayGameRequested;

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

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
            EnsureHomeReferences();

            _hasConstructed = true;

            if (_popupManager == null)
            {
                Debug.LogError("PopupManager is missing.");
            }
        }

        public void ShowHome()
        {
            EnsureHomeReferences();

            if (_mainMenuRoot != null)
            {
                _mainMenuRoot.gameObject.SetActive(true);
            }

            if (_homeView != null)
            {
                _homeView.Show();
                _homeView.SetActions(new HomeViewActions(OnHomePlayRequested));
                return;
            }

            if (_popupManager == null)
            {
                Debug.LogError("Cannot show home because HomeView and PopupManager are missing.");
                return;
            }

            HomeView homeView = _popupManager.Show<HomeView>();

            if (homeView == null)
            {
                return;
            }

            homeView.SetActions(new HomeViewActions(OnHomePlayRequested));
        }

        public void HideHome()
        {
            EnsureHomeReferences();

            if (_mainMenuRoot != null)
            {
                _mainMenuRoot.gameObject.SetActive(false);
                return;
            }

            if (_homeView != null)
            {
                _homeView.Hide();
                return;
            }

            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<HomeView>();
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

        private void EnsureHomeReferences()
        {
            Transform searchRoot = transform.parent != null ? transform.parent : transform;

            if (_homeView == null)
            {
                _homeView = searchRoot.GetComponentInChildren<HomeView>(true);
            }

            if (_mainMenuRoot != null)
            {
                return;
            }

            Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "MainMenuRoot")
                {
                    _mainMenuRoot = transforms[i];
                    return;
                }
            }
        }

        private void OnHomePlayRequested()
        {
            PlayGameRequested?.Invoke();
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
