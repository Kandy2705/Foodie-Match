using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.Infrastructure.Audio;
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
        [SerializeField]
        private GameplayMotionPresenter
            _gameplayMotionPresenter;

        [Header("Audio")]
        [SerializeField] private UnityAudioService _audioService;

        [Header("Gameplay Roots")]
        [SerializeField] private GameplayPoolRoot _gameplayPoolRoot;

        [SerializeField] private BoardLayoutView _boardLayoutView;

        [SerializeField] private RequiredPackageGroupView _requiredPackageGroupView;

        [SerializeField] private WaitingRackView _waitingRackView;

        [SerializeField] private FoodVisualResolver _foodVisualResolver;
        [SerializeField] private FridgeBoosterAnchors _fridgeBoosterAnchors;
        [SerializeField] private GameplayWorldClickSfx _gameplayWorldClickSfx;

        private CancellationTokenSource _initializationCancellation;

        public AppInstaller AppInstaller => _appInstaller;
        public AppController AppController => _appController;
        public GameplayController GameplayController => _gameplayController;
        public UIManager UIManager => _uiManager;
        public GameplayMotionPresenter GameplayMotionPresenter =>
            _gameplayMotionPresenter;
        public UnityAudioService AudioService => _audioService;
        public GameplayPoolRoot GameplayPoolRoot => _gameplayPoolRoot;
        public BoardLayoutView BoardLayoutView => _boardLayoutView;
        public RequiredPackageGroupView RequiredPackageGroupView => _requiredPackageGroupView;
        public WaitingRackView WaitingRackView => _waitingRackView;
        public FoodVisualResolver FoodVisualResolver => _foodVisualResolver;
        public FridgeBoosterAnchors FridgeBoosterAnchors => _fridgeBoosterAnchors;
        public GameplayWorldClickSfx GameplayWorldClickSfx => _gameplayWorldClickSfx;

        private void OnDestroy()
        {
            _initializationCancellation?.Cancel();
        }

        public void Initialize()
        {
            if (!_appInstaller.Install(this))
            {
                return;
            }

            _initializationCancellation = new CancellationTokenSource();
            _ = InitializeSafelyAsync(_initializationCancellation.Token);
        }

        private async Task InitializeSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task loadingTask = _uiManager.PlayLoadingAsync();
                await _appInstaller.GameConfigurationLoader.RefreshAsync(cancellationToken);
                PlayerProfileInitializationResult result =
                    await _appInstaller.PlayerProfileInitializer.InitializeAsync(
                        cancellationToken);
                await loadingTask;

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
                await _appController.EnterHomeAsync();
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
                if (!cancellationToken.IsCancellationRequested)
                {
                    await _uiManager.HideLoadingAsync();
                }

                _initializationCancellation?.Dispose();
                _initializationCancellation = null;
            }
        }
    }
}
