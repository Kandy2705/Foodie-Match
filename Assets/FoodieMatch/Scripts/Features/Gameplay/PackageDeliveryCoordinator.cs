using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.RequiredPackage;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class PackageDeliveryCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayMotionPresenter _motionPresenter;
        private readonly GameplayAudioPresenter _audioPresenter;
        private readonly RequiredPackageLifecycleUseCase _packageLifecycleUseCase;
        private readonly RequiredPackageGroupView _packageGroupView;
        private readonly FoodVisualResolver _foodVisualResolver;
        private readonly GameplayEvents _gameplayEvents;

        private GameplaySession _session;
        private PackageMotionState[] _motionStates;

        public event Action<GameplaySession> PackageReplaced;
        public event Action<GameplaySession> PackageDeliveryFailed;

        public PackageDeliveryCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayMotionPresenter motionPresenter,
            GameplayAudioPresenter audioPresenter,
            RequiredPackageLifecycleUseCase packageLifecycleUseCase,
            RequiredPackageGroupView packageGroupView,
            FoodVisualResolver foodVisualResolver,
            GameplayEvents gameplayEvents)
        {
            _sessionGuard = sessionGuard;
            _motionPresenter = motionPresenter;
            _audioPresenter = audioPresenter;
            _packageLifecycleUseCase = packageLifecycleUseCase;
            _packageGroupView = packageGroupView;
            _foodVisualResolver = foodVisualResolver;
            _gameplayEvents = gameplayEvents;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
            _packageGroupView.ResetLayout();
            _motionStates = new PackageMotionState[session.RequiredPackages.Length];

            for (int i = 0; i < session.RequiredPackages.Length; i++)
            {
                _motionStates[i] = new(session.RequiredPackages[i]);
            }

            RefreshPackageViews();
        }

        public void EndSession()
        {
            _session = null;
            _motionStates = null;
        }

        public async Task DeliverSelectedFoodAsync(
            FoodItemView foodItemView,
            int packageIndex,
            GameplaySession session)
        {
            if (!CanContinue(session))
            {
                return;
            }

            IncreaseServedFoodCount(session);

            if (!TryCreateSelectedFoodFlight(
                    foodItemView, packageIndex, session, out PackageFlight flight))
            {
                Debug.LogError("Required package flight could not be created.");
                foodItemView?.Clear();
                RefreshPackageViewIfValid(packageIndex, session);
                IncreaseDisplayedServedFoodCount(session);
                OnPackageDeliveryFailed(session);
                return;
            }

            if (!TryRegisterFlight(flight, out _))
            {
                Debug.LogError($"Required package {flight.PackageIndex} could not register an incoming flight.");
                ReconcileFailedFlight(flight);
                IncreaseDisplayedServedFoodCount(session);
                OnPackageDeliveryFailed(session);
                return;
            }

            await ProcessFlightAsync(flight, session);
        }

        public bool TryCreateWaitingRackFlight(
            WaitingRackTransfer transfer,
            FoodItemView foodItemView,
            GameplaySession session,
            out PackageFlight flight)
        {
            flight = default;

            if (!CanContinue(session) ||
                foodItemView == null ||
                foodItemView.FoodTokenId != transfer.FoodTokenId ||
                transfer.PackageIndex < 0 ||
                transfer.PackageIndex >= session.RequiredPackages.Length ||
                transfer.PackageIndex >= _motionStates.Length)
            {
                return false;
            }

            RequiredPackageModel expectedPackage = session.RequiredPackages[transfer.PackageIndex];
            PackageMotionState motionState = _motionStates[transfer.PackageIndex];

            if (expectedPackage == null ||
                !expectedPackage.CanAccept(transfer.FoodTokenId) ||
                motionState == null ||
                motionState.Package != expectedPackage ||
                motionState.IsReplacementRunning)
            {
                return false;
            }

            flight = new(
                foodItemView, expectedPackage, transfer.PackageIndex,
                expectedPackage.RequiredAmount, expectedPackage.FilledAmount);

            return true;
        }

        public async Task<bool> DeliverBatchAsync(IReadOnlyList<PackageFlight> flights, GameplaySession session)
        {
            if (!CanContinue(session) || flights == null)
            {
                return false;
            }

            if (!TryRegisterFlights(flights))
            {
                Debug.LogError("Waiting rack auto-fill flights could not be registered.");
                ReconcileUnlaunchedFlights(flights, session);
                OnPackageDeliveryFailed(session);
                return false;
            }

            Task[] motionTasks = new Task[flights.Count];

            for (int i = 0; i < flights.Count; i++)
            {
                motionTasks[i] = ProcessFlightAsync(flights[i], session);
            }

            await Task.WhenAll(motionTasks);
            return true;
        }

        public void IncreaseServedFoodCount(GameplaySession session)
        {
            if (_session != session || !session.Progress.TryServeFood())
            {
                Debug.LogError("Level progress could not serve food.");
            }
        }

        public bool HasActiveMotion(GameplaySession session)
        {
            if (_session != session || _motionStates == null)
            {
                return false;
            }

            for (int i = 0; i < _motionStates.Length; i++)
            {
                PackageMotionState motionState = _motionStates[i];

                if (motionState != null &&
                    (motionState.IncomingFlightCount > 0 ||
                     motionState.IsReplacementRunning ||
                     motionState.Package != null && motionState.Package.IsComplete))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task ProcessFlightAsync(PackageFlight flight, GameplaySession session)
        {
            if (!TryGetMotionState(flight.PackageIndex, flight.ExpectedPackage, out PackageMotionState motionState))
            {
                return;
            }

            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.MoveFoodToRequiredPackageAsync(
                    flight.FoodItemView,
                    flight.PackageIndex,
                    flight.RequiredAmount,
                    flight.FilledSlotIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }
            finally
            {
                if (!motionState.TryCompleteIncomingFlight(flight.ExpectedPackage))
                {
                    Debug.LogError(
                        $"Required package {flight.PackageIndex} could not complete an incoming flight.");
                }
            }

            if (!CanContinue(session) || !IsExpectedPackage(flight))
            {
                return;
            }

            if (motionResult == MotionResult.Cancelled)
            {
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError($"Food flight to required package {flight.PackageIndex} failed.");
                ReconcileFailedFlight(flight);
            }

            IncreaseDisplayedServedFoodCount(session);
            TryStartPackageReplacement(flight.PackageIndex, flight.ExpectedPackage, session);
        }

        private bool TryCreateSelectedFoodFlight(
            FoodItemView foodItemView,
            int packageIndex,
            GameplaySession session,
            out PackageFlight flight)
        {
            flight = default;

            if (!CanContinue(session) ||
                foodItemView == null ||
                packageIndex < 0 ||
                packageIndex >= session.RequiredPackages.Length ||
                packageIndex >= _motionStates.Length)
            {
                return false;
            }

            RequiredPackageModel requiredPackage = session.RequiredPackages[packageIndex];

            if (requiredPackage == null || requiredPackage.FilledAmount <= 0)
            {
                return false;
            }

            flight = new(
                foodItemView, requiredPackage, packageIndex,
                requiredPackage.RequiredAmount, requiredPackage.FilledAmount - 1);

            return true;
        }

        private bool TryRegisterFlights(IReadOnlyList<PackageFlight> flights)
        {
            int registeredFlightCount = 0;

            for (int i = 0; i < flights.Count; i++)
            {
                if (!TryRegisterFlight(flights[i], out _))
                {
                    RollbackRegisteredFlights(flights, registeredFlightCount);
                    return false;
                }

                registeredFlightCount++;
            }

            return true;
        }

        private bool TryRegisterFlight(PackageFlight flight, out PackageMotionState motionState)
        {
            return TryGetMotionState(flight.PackageIndex, flight.ExpectedPackage, out motionState) &&
                   motionState.TryRegisterIncomingFlight(flight.ExpectedPackage);
        }

        private void RollbackRegisteredFlights(IReadOnlyList<PackageFlight> flights, int registeredFlightCount)
        {
            for (int i = 0; i < registeredFlightCount; i++)
            {
                PackageFlight flight = flights[i];
                PackageMotionState motionState = _motionStates[flight.PackageIndex];
                motionState.TryCompleteIncomingFlight(flight.ExpectedPackage);
            }
        }

        private void ReconcileUnlaunchedFlights(IReadOnlyList<PackageFlight> flights, GameplaySession session)
        {
            for (int i = 0; i < flights.Count; i++)
            {
                ReconcileFailedFlight(flights[i]);
                IncreaseDisplayedServedFoodCount(session);
            }
        }

        private void ReconcileFailedFlight(PackageFlight flight)
        {
            flight.FoodItemView.Clear();
            RefreshPackageViewAt(flight.PackageIndex);
        }

        private void IncreaseDisplayedServedFoodCount(GameplaySession session)
        {
            if (_session != session || !session.TryIncreaseDisplayedServedCount())
            {
                return;
            }

            _gameplayEvents.OnLevelProgressChanged(
                new LevelProgressChangedEvent(session.DisplayedServedCount, session.Progress.TotalCount));
        }

        private void TryStartPackageReplacement(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            GameplaySession session)
        {
            if (!CanContinue(session) ||
                !TryGetMotionState(packageIndex, expectedPackage, out PackageMotionState motionState) ||
                !expectedPackage.IsComplete ||
                motionState.IncomingFlightCount != 0 ||
                motionState.IsReplacementRunning)
            {
                return;
            }

            motionState.IsReplacementRunning = true;
            _ = ReplacePackageSafelyAsync(packageIndex, expectedPackage, session);
        }

        private async Task ReplacePackageSafelyAsync(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            GameplaySession session)
        {
            MotionResult matchMotionResult = await PlayPackageMatchSafelyAsync(packageIndex);

            if (!CanContinue(session) ||
                !TryGetMotionState(packageIndex, expectedPackage, out PackageMotionState motionState))
            {
                return;
            }

            if (matchMotionResult == MotionResult.Cancelled)
            {
                motionState.IsReplacementRunning = false;
                return;
            }

            if (matchMotionResult == MotionResult.Failed)
            {
                Debug.LogError($"Required package {packageIndex} match motion failed.");
            }

            IReadOnlyList<RequiredPackageModel> packageReservations = CreatePackageReservations();

            if (!_packageLifecycleUseCase.TryPrepareReplacementPackage(
                    packageIndex, session.Board, session.WaitingRack, session.RequiredPackages,
                    packageReservations, session.PackageSettings, out RequiredPackageModel replacementPackage))
            {
                motionState.IsReplacementRunning = false;
                RefreshPackageViewAt(packageIndex);
                Debug.LogError($"Required package {packageIndex} replacement could not be prepared.");
                return;
            }

            if (replacementPackage != null &&
                (!motionState.TrySetPendingPackage(expectedPackage, replacementPackage) ||
                 !ShowPackageViewAt(packageIndex, replacementPackage)))
            {
                motionState.CancelReplacement(expectedPackage);
                RefreshPackageViewAt(packageIndex);
                Debug.LogError($"Required package {packageIndex} pending view could not be prepared.");
                return;
            }

            if (replacementPackage != null)
            {
                MotionResult enterMotionResult = await PlayPackageEnterSafelyAsync(packageIndex);

                if (!CanContinue(session) ||
                    !TryGetMotionState(packageIndex, expectedPackage, out motionState) ||
                    motionState.PendingPackage != replacementPackage)
                {
                    return;
                }

                if (enterMotionResult == MotionResult.Cancelled)
                {
                    motionState.CancelReplacement(expectedPackage);
                    RefreshPackageViewAt(packageIndex);
                    return;
                }

                if (enterMotionResult == MotionResult.Failed)
                {
                    Debug.LogError($"Required package {packageIndex} enter motion failed.");
                    _packageGroupView.GetPackageAt(packageIndex)?.StopMotion();
                }
            }

            if (!_packageLifecycleUseCase.TryPublishReplacementPackage(
                    packageIndex, expectedPackage, replacementPackage, session.RequiredPackages))
            {
                motionState.CancelReplacement(expectedPackage);
                RefreshPackageViewAt(packageIndex);
                Debug.LogError($"Required package {packageIndex} replacement could not be published.");
                return;
            }

            if (replacementPackage == null)
            {
                RefreshPackageViewAt(packageIndex);

                MotionResult layoutMotionResult = await RecenterPackagesSafelyAsync();

                if (!CanContinue(session) ||
                    !IsCurrentMotionState(packageIndex, expectedPackage, motionState))
                {
                    return;
                }

                if (layoutMotionResult == MotionResult.Cancelled)
                {
                    return;
                }

                if (layoutMotionResult == MotionResult.Failed)
                {
                    Debug.LogError("Required package layout motion failed.");
                }
            }

            motionState.Reset(replacementPackage);
            PackageReplaced?.Invoke(session);
        }

        private async Task<MotionResult> PlayPackageMatchSafelyAsync(int packageIndex)
        {
            try
            {
                return await _motionPresenter.PlayRequiredPackageMatchAsync(
                    packageIndex,
                    _audioPresenter.PlayPackageCompleted,
                    _audioPresenter.PlayPackageLidClosed);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return MotionResult.Failed;
            }
        }

        private async Task<MotionResult> PlayPackageEnterSafelyAsync(int packageIndex)
        {
            try
            {
                return await _motionPresenter.PlayRequiredPackageEnterAsync(
                    packageIndex,
                    _audioPresenter.PlayPackageEntering);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return MotionResult.Failed;
            }
        }

        private async Task<MotionResult> RecenterPackagesSafelyAsync()
        {
            try
            {
                return await _motionPresenter.RecenterRequiredPackagesAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return MotionResult.Failed;
            }
        }

        private IReadOnlyList<RequiredPackageModel> CreatePackageReservations()
        {
            RequiredPackageModel[] packageReservations = new RequiredPackageModel[_session.RequiredPackages.Length];

            for (int i = 0; i < packageReservations.Length; i++)
            {
                PackageMotionState motionState = _motionStates[i];
                packageReservations[i] = motionState?.PendingPackage ?? _session.RequiredPackages[i];
            }

            return packageReservations;
        }

        private bool TryGetMotionState(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            out PackageMotionState motionState)
        {
            motionState = null;

            if (_session == null ||
                _motionStates == null ||
                packageIndex < 0 ||
                packageIndex >= _session.RequiredPackages.Length ||
                packageIndex >= _motionStates.Length ||
                _session.RequiredPackages[packageIndex] != expectedPackage)
            {
                return false;
            }

            motionState = _motionStates[packageIndex];
            return motionState != null && motionState.Package == expectedPackage;
        }

        private bool IsExpectedPackage(PackageFlight flight)
        {
            return TryGetMotionState(flight.PackageIndex, flight.ExpectedPackage, out _);
        }

        private bool IsCurrentMotionState(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            PackageMotionState expectedMotionState)
        {
            return _motionStates != null &&
                   packageIndex >= 0 &&
                   packageIndex < _motionStates.Length &&
                   _motionStates[packageIndex] == expectedMotionState &&
                   expectedMotionState.Package == expectedPackage;
        }

        private bool CanContinue(GameplaySession session)
        {
            return session != null &&
                   _session == session &&
                   _sessionGuard.IsCurrentSession(session.SessionId) &&
                   session.CanContinueGameplay;
        }

        private void RefreshPackageViews()
        {
            for (int i = 0; i < _session.RequiredPackages.Length; i++)
            {
                RefreshPackageViewAt(i);
            }
        }

        private void RefreshPackageViewIfValid(int packageIndex, GameplaySession session)
        {
            if (_session == session &&
                packageIndex >= 0 &&
                packageIndex < session.RequiredPackages.Length)
            {
                RefreshPackageViewAt(packageIndex);
            }
        }

        private void RefreshPackageViewAt(int packageIndex)
        {
            RequiredPackageModel package = _session.RequiredPackages[packageIndex];
            if (!ShowPackageViewAt(packageIndex, package))
            {
                Debug.LogError($"Required package view {packageIndex} could not be updated.");
            }
        }

        private bool ShowPackageViewAt(int packageIndex, RequiredPackageModel package)
        {
            Sprite sprite = package != null ? _foodVisualResolver.ResolveIcon(package.FoodTokenId) : null;
            return _packageGroupView.ShowPackageAt(packageIndex, package, sprite);
        }

        private void OnPackageDeliveryFailed(GameplaySession session)
        {
            if (_session == session)
            {
                PackageDeliveryFailed?.Invoke(session);
            }
        }

        private sealed class PackageMotionState
        {
            public PackageMotionState(RequiredPackageModel package)
            {
                Package = package;
            }

            public RequiredPackageModel Package { get; private set; }
            public RequiredPackageModel PendingPackage { get; private set; }
            public int IncomingFlightCount { get; private set; }
            public bool IsReplacementRunning { get; set; }

            public bool TryRegisterIncomingFlight(RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage || IsReplacementRunning)
                {
                    return false;
                }

                IncomingFlightCount++;
                return true;
            }

            public bool TryCompleteIncomingFlight(RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage || IncomingFlightCount <= 0)
                {
                    return false;
                }

                IncomingFlightCount--;
                return true;
            }

            public bool TrySetPendingPackage(
                RequiredPackageModel expectedPackage,
                RequiredPackageModel pendingPackage)
            {
                if (Package != expectedPackage ||
                    pendingPackage == null ||
                    PendingPackage != null ||
                    !IsReplacementRunning)
                {
                    return false;
                }

                PendingPackage = pendingPackage;
                return true;
            }

            public void CancelReplacement(RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage)
                {
                    return;
                }

                PendingPackage = null;
                IsReplacementRunning = false;
            }

            public void Reset(RequiredPackageModel package)
            {
                Package = package;
                PendingPackage = null;
                IncomingFlightCount = 0;
                IsReplacementRunning = false;
            }
        }
    }
}
