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

        [SerializeField] private Transform _gameplayTopRoot;

        [SerializeField] private BoardLayoutView _boardLayoutView;

        [SerializeField] private RequiredPackageGroupView _requiredPackageGroupView;

        [SerializeField] private WaitingRackView _waitingRackView;

        [SerializeField] private FoodVisualResolver _foodVisualResolver;
        [SerializeField] private FridgeBoosterAnchors _fridgeBoosterAnchors;
        [SerializeField] private GameplayPointerInput _gameplayPointerInput;
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
        public Transform GameplayTopRoot => _gameplayTopRoot;
        public BoardLayoutView BoardLayoutView => _boardLayoutView;
        public RequiredPackageGroupView RequiredPackageGroupView => _requiredPackageGroupView;
        public WaitingRackView WaitingRackView => _waitingRackView;
        public FoodVisualResolver FoodVisualResolver => _foodVisualResolver;
        public FridgeBoosterAnchors FridgeBoosterAnchors => _fridgeBoosterAnchors;
        public GameplayPointerInput GameplayPointerInput => _gameplayPointerInput;
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
                _uiManager.ShowLoadingImmediately();
                _uiManager.SetLoadingProgress(0.2f);
                Task<bool> authenticationTask =
                    _appInstaller.PlayerIdentityService.AuthenticateAsync(
                        cancellationToken);
                await _appInstaller.GameConfigurationLoader.RefreshAsync(
                    cancellationToken);

                _uiManager.SetLoadingProgress(0.35f);
                Task<PlayerProfileInitializationResult> profileTask =
                    _appInstaller.PlayerProfileInitializer.InitializeAsync(
                        cancellationToken);
                PlayerProfileInitializationResult result =
                    await profileTask;
                _uiManager.SetLoadingProgress(0.4f);

                if (result.IsSuccess)
                {
                    PlayerProfileRecord synchronizedRecord =
                        await SynchronizePlayerProfileWithinStartupLimitAsync(
                            authenticationTask,
                            result.Record,
                            startupTimer,
                            cancellationToken);
                    result = PlayerProfileInitializationResult.Succeeded(
                        synchronizedRecord,
                        result.RecoveredInvalidData);
                }

                LevelLoadingProgressReporter levelProgress = new(
                    _uiManager,
                    checkingManifest: 0.45f,
                    manifestReady: 0.55f,
                    packsReady: 0.87f,
                    completed: 0.9f);
                Task levelSynchronizationTask = result.IsSuccess
                    ? _appInstaller.LevelSynchronizer
                        .SynchronizeUpcomingLevelsAsync(
                            result.Record.Profile.CurrentLevelNumber,
                            LevelSynchronizationSettings.FollowingLevelCount,
                            levelProgress.Report,
                            cancellationToken)
                    : Task.CompletedTask;
                Task startupWorkTask = levelSynchronizationTask;
                await WaitForStartupWorkAsync(
                    startupWorkTask,
                    startupTimer.Elapsed,
                    cancellationToken);
                levelProgress.Stop();
                _uiManager.SetLoadingProgress(0.92f);

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
                _uiManager.SetLoadingProgress(0.97f);
                await _appController.EnterStartupDestinationAsync();
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

        private async Task<PlayerProfileRecord>
            SynchronizePlayerProfileWithinStartupLimitAsync(
                Task<bool> authenticationTask,
                PlayerProfileRecord localRecord,
                Stopwatch startupTimer,
                CancellationToken cancellationToken)
        {
            TimeSpan remainingDuration =
                LevelSynchronizationSettings.LoadingWaitLimit -
                startupTimer.Elapsed;

            if (remainingDuration <= TimeSpan.Zero)
            {
                _ = ObserveStartupWorkAsync(
                    authenticationTask,
                    cancellationToken);
                return localRecord;
            }

            using CancellationTokenSource syncCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);
            Task<PlayerProfileRecord> synchronizationTask =
                SynchronizeAfterAuthenticationAsync(
                    authenticationTask,
                    localRecord,
                    syncCancellation.Token);
            Task completedTask = await Task.WhenAny(
                synchronizationTask,
                Task.Delay(remainingDuration, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();

            if (completedTask == synchronizationTask)
            {
                return await synchronizationTask;
            }

            syncCancellation.Cancel();
            _ = ObserveProfileSynchronizationAsync(synchronizationTask);
            return localRecord;
        }

        private async Task<PlayerProfileRecord>
            SynchronizeAfterAuthenticationAsync(
                Task<bool> authenticationTask,
                PlayerProfileRecord localRecord,
                CancellationToken cancellationToken)
        {
            bool isAuthenticated = await authenticationTask;
            cancellationToken.ThrowIfCancellationRequested();

            return isAuthenticated
                ? await _appInstaller.PlayerProfileCloudSynchronizer
                    .SynchronizeAsync(cancellationToken)
                : localRecord;
        }

        private static async Task ObserveProfileSynchronizationAsync(
            Task synchronizationTask)
        {
            try
            {
                await synchronizationTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Player profile cloud synchronization failed: " +
                    exception.Message);
            }
        }

        private static async Task WaitForStartupWorkAsync(
            Task startupWorkTask,
            TimeSpan elapsed,
            CancellationToken cancellationToken)
        {
            TimeSpan remainingDuration =
                LevelSynchronizationSettings.LoadingWaitLimit - elapsed;

            if (remainingDuration <= TimeSpan.Zero)
            {
                _ = ObserveStartupWorkAsync(
                    startupWorkTask,
                    cancellationToken);
                return;
            }

            Task completedTask = await Task.WhenAny(
                startupWorkTask,
                Task.Delay(
                    remainingDuration,
                    cancellationToken));

            if (completedTask == startupWorkTask)
            {
                await ObserveStartupWorkAsync(
                    startupWorkTask,
                    cancellationToken);
                return;
            }

            _ = ObserveStartupWorkAsync(
                startupWorkTask,
                cancellationToken);
        }

        private static async Task ObserveStartupWorkAsync(
            Task startupWorkTask,
            CancellationToken cancellationToken)
        {
            try
            {
                await startupWorkTask;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Startup background work failed: {exception.Message}");
            }
        }
    }

    internal sealed class LevelLoadingProgressReporter
    {
        private const float ActiveDownloadStepProgress = 0.85f;

        private readonly UIManager _uiManager;
        private readonly float _checkingManifest;
        private readonly float _manifestReady;
        private readonly float _packsReady;
        private readonly float _completed;

        private bool _isActive = true;

        public LevelLoadingProgressReporter(
            UIManager uiManager,
            float checkingManifest,
            float manifestReady,
            float packsReady,
            float completed)
        {
            _uiManager = uiManager;
            _checkingManifest = checkingManifest;
            _manifestReady = manifestReady;
            _packsReady = packsReady;
            _completed = completed;
        }

        public void Report(LevelSynchronizationProgress progress)
        {
            if (!_isActive)
            {
                return;
            }

            float loadingProgress = progress.Stage switch
            {
                LevelSynchronizationStage.CheckingManifest => _checkingManifest,
                LevelSynchronizationStage.ManifestReady => _manifestReady,
                LevelSynchronizationStage.DownloadingPacks => MapPackProgress(
                    progress,
                    _manifestReady,
                    _packsReady),
                LevelSynchronizationStage.Completed => _completed,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    progress.Stage,
                    null)
            };

            _uiManager.SetLoadingProgress(loadingProgress);
        }

        public void Stop()
        {
            _isActive = false;
        }

        private static float MapPackProgress(
            LevelSynchronizationProgress progress,
            float manifestReady,
            float packsReady)
        {
            if (progress.TotalPackCount == 0)
            {
                return packsReady;
            }

            float completedRatio =
                (float)progress.CompletedPackCount /
                progress.TotalPackCount;

            if (progress.CompletedPackCount >= progress.TotalPackCount)
            {
                return packsReady;
            }

            float nextPackRatio =
                (float)(progress.CompletedPackCount + 1) /
                progress.TotalPackCount;
            float activeRatio = Mathf.Lerp(
                completedRatio,
                nextPackRatio,
                ActiveDownloadStepProgress);
            return Mathf.Lerp(manifestReady, packsReady, activeRatio);
        }
    }
}
