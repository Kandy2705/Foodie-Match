using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppRoot : MonoBehaviour
    {
        [Header("Installers")]
        [SerializeField] private AppInstaller _appInstaller;

        [Header("Controllers")]
        [SerializeField] private AppController _appController;

        [SerializeField] private GameplayController _gameplayController;

        [SerializeField] private UIManager _uiManager;

        [Header("Presentation")]
        [SerializeField] private GameplayMotionPresenter
            _gameplayMotionPresenter;

        [Header("Audio")]
        [SerializeField] private UnityAudioService _audioService;

        [Header("Gameplay Roots")]
        [SerializeField] private BoardLayoutView _boardLayoutView;

        [SerializeField] private RequiredPackageGroupView _requiredPackageGroupView;

        [SerializeField] private WaitingRackView _waitingRackView;

        [SerializeField] private FoodVisualResolver _foodVisualResolver;

        private CancellationTokenSource _initializationCancellation;
        private bool _isInitializing;
        private bool _isInitialized;
        private bool _isDestroyed;

        public AppInstaller AppInstaller => _appInstaller;
        public AppController AppController => _appController;
        public GameplayController GameplayController => _gameplayController;
        public UIManager UIManager => _uiManager;
        public GameplayMotionPresenter GameplayMotionPresenter =>
            _gameplayMotionPresenter;
        public UnityAudioService AudioService => _audioService;
        public BoardLayoutView BoardLayoutView => _boardLayoutView;
        public RequiredPackageGroupView RequiredPackageGroupView => _requiredPackageGroupView;
        public WaitingRackView WaitingRackView => _waitingRackView;
        public FoodVisualResolver FoodVisualResolver => _foodVisualResolver;

        private void OnDestroy()
        {
            _isDestroyed = true;
            _initializationCancellation?.Cancel();
        }

        public void Initialize()
        {
            if (_isInitializing || _isInitialized)
            {
                return;
            }

            if (!HasValidReferences())
            {
                return;
            }

            if (!_appInstaller.Install(this))
            {
                return;
            }

            _isInitializing = true;
            _initializationCancellation = new CancellationTokenSource();
            _ = InitializeSafelyAsync(_initializationCancellation.Token);
        }

        private async Task InitializeSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task loadingTask = _uiManager.PlayLoadingAsync();
                Task<PlayerProfileInitializationResult> profileTask =
                    _appInstaller.PlayerProfileInitializer.InitializeAsync(cancellationToken);

                await Task.WhenAll(loadingTask, profileTask);
                PlayerProfileInitializationResult result = await profileTask;

                if (!result.IsSuccess)
                {
                    Debug.LogError(
                        $"Player profile initialization failed: {result.ErrorMessage}");
                    return;
                }

                if (result.RecoveredInvalidData)
                {
                    Debug.LogWarning(
                        "Invalid player profile was backed up and replaced.");
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (_isDestroyed)
                {
                    return;
                }

                _appController.EnterHome();
                _isInitialized = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (!_isDestroyed)
                {
                    _uiManager.HideLoading();
                }

                _initializationCancellation?.Dispose();
                _initializationCancellation = null;
                _isInitializing = false;
            }
        }

        private bool HasValidReferences()
        {
            if (_appInstaller == null)
            {
                Debug.LogError("AppInstaller is missing.");
                return false;
            }

            if (_appController == null)
            {
                Debug.LogError("AppController is missing.");
                return false;
            }

            if (_gameplayController == null)
            {
                Debug.LogError("GameplayController is missing.");
                return false;
            }

            if (_uiManager == null)
            {
                Debug.LogError("UIManager is missing.");
                return false;
            }

            if (_audioService == null)
            {
                Debug.LogWarning(
                    "UnityAudioService is missing. Audio will use NullAudioService.");
            }

            if (_gameplayMotionPresenter == null)
            {
                Debug.LogError("GameplayMotionPresenter is missing.");
                return false;
            }

            if (_boardLayoutView == null)
            {
                Debug.LogError("BoardLayoutView is missing.");
                return false;
            }

            if (_requiredPackageGroupView == null)
            {
                Debug.LogError("RequiredPackageGroupView is missing.");
                return false;
            }

            if (_waitingRackView == null)
            {
                Debug.LogError("WaitingRackView is missing.");
                return false;
            }

            if (_foodVisualResolver == null)
            {
                Debug.LogError("FoodVisualResolver is missing.");
                return false;
            }

            return true;
        }
    }
}

