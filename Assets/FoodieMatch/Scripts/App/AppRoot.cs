using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Level;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.Infrastructure.Audio;
using FoodieMatch.UI;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

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
            _initializationCancellation?.Dispose();
            _initializationCancellation = null;
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
                Stopwatch startupTimer = Stopwatch.StartNew();
                Task loadingTask = _uiManager.PlayLoadingAsync();
                await _appInstaller.GameConfigurationLoader.RefreshAsync(
                    cancellationToken);

                Task<PlayerProfileInitializationResult> profileTask =
                    _appInstaller.PlayerProfileInitializer.InitializeAsync(
                        cancellationToken);
                PlayerProfileInitializationResult result =
                    await profileTask;
                Task levelSynchronizationTask = result.IsSuccess
                    ? _appInstaller.LevelSynchronizer
                        .SynchronizeUpcomingLevelsAsync(
                            result.Record.Profile.CurrentLevelNumber,
                            LevelSynchronizationSettings.FollowingLevelCount,
                            cancellationToken)
                    : Task.CompletedTask;
                await WaitForStartupLevelSynchronizationAsync(
                    levelSynchronizationTask,
                    startupTimer.Elapsed,
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
            }
        }

        private static async Task WaitForStartupLevelSynchronizationAsync(
            Task synchronizationTask,
            TimeSpan elapsed,
            CancellationToken cancellationToken)
        {
            TimeSpan remainingDuration =
                LevelSynchronizationSettings.LoadingWaitLimit - elapsed;

            if (remainingDuration <= TimeSpan.Zero)
            {
                _ = ObserveLevelSynchronizationAsync(
                    synchronizationTask,
                    cancellationToken);
                return;
            }

            Task completedTask = await Task.WhenAny(
                synchronizationTask,
                Task.Delay(
                    remainingDuration,
                    cancellationToken));

            if (completedTask == synchronizationTask)
            {
                await ObserveLevelSynchronizationAsync(
                    synchronizationTask,
                    cancellationToken);
                return;
            }

            _ = ObserveLevelSynchronizationAsync(
                synchronizationTask,
                cancellationToken);
        }

        private static async Task ObserveLevelSynchronizationAsync(
            Task synchronizationTask,
            CancellationToken cancellationToken)
        {
            try
            {
                await synchronizationTask;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Startup level synchronization failed: {exception.Message}");
            }
        }
    }
}
