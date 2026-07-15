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
            RequiredPackageLifecycleUseCase packageLifecycleUseCase,
            RequiredPackageGroupView packageGroupView,
            FoodVisualResolver foodVisualResolver,
            GameplayEvents gameplayEvents)
        {
            _sessionGuard = sessionGuard;
            _motionPresenter = motionPresenter;
            _packageLifecycleUseCase = packageLifecycleUseCase;
            _packageGroupView = packageGroupView;
            _foodVisualResolver = foodVisualResolver;
            _gameplayEvents = gameplayEvents;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
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
                    foodItemView,
                    packageIndex,
                    session,
                    out PackageFlight flight))
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
                Debug.LogError(
                    $"Required package {flight.PackageIndex} could not register an incoming flight.");
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
                motionState.IsCompleteMotionRunning)
            {
                return false;
            }

            flight = new(
                foodItemView,
                expectedPackage,
                transfer.PackageIndex,
                expectedPackage.RequiredAmount,
                expectedPackage.FilledAmount);

            return true;
        }

        public async Task<bool> DeliverBatchAsync(
            IReadOnlyList<PackageFlight> flights,
            GameplaySession session)
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
                     motionState.IsCompleteMotionRunning ||
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
            TryStartPackageCompletion(flight.PackageIndex, flight.ExpectedPackage, session);
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
                foodItemView,
                requiredPackage,
                packageIndex,
                requiredPackage.RequiredAmount,
                requiredPackage.FilledAmount - 1);

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

        private void ReconcileUnlaunchedFlights(
            IReadOnlyList<PackageFlight> flights,
            GameplaySession session)
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
                new LevelProgressChangedEvent(
                    session.DisplayedServedCount,
                    session.Progress.TotalCount));
        }

        private void TryStartPackageCompletion(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            GameplaySession session)
        {
            if (!CanContinue(session) ||
                !TryGetMotionState(packageIndex, expectedPackage, out PackageMotionState motionState) ||
                !expectedPackage.IsComplete ||
                motionState.IncomingFlightCount != 0 ||
                motionState.IsCompleteMotionRunning)
            {
                return;
            }

            motionState.IsCompleteMotionRunning = true;
            _ = CompletePackageSafelyAsync(packageIndex, expectedPackage, session);
        }

        private async Task CompletePackageSafelyAsync(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.PlayRequiredPackageCompleteAsync(packageIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }

            if (!CanContinue(session) ||
                !TryGetMotionState(packageIndex, expectedPackage, out PackageMotionState motionState))
            {
                return;
            }

            if (motionResult == MotionResult.Cancelled)
            {
                motionState.IsCompleteMotionRunning = false;
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError($"Required package {packageIndex} complete feedback failed.");
            }

            if (!_packageLifecycleUseCase.TryReplaceCompletedPackage(
                    packageIndex,
                    session.Board,
                    session.WaitingRack,
                    session.RequiredPackages,
                    session.PackageSettings,
                    out RequiredPackageModel newPackage))
            {
                motionState.IsCompleteMotionRunning = false;
                Debug.LogError($"Required package {packageIndex} could not be replaced.");
                return;
            }

            motionState.Reset(newPackage);
            RefreshPackageViewAt(packageIndex);
            PackageReplaced?.Invoke(session);
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
            Sprite sprite = package != null
                ? _foodVisualResolver.ResolveIcon(package.FoodTokenId)
                : null;

            if (!_packageGroupView.ShowPackageAt(packageIndex, package, sprite))
            {
                Debug.LogError($"Required package view {packageIndex} could not be updated.");
            }
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
            public int IncomingFlightCount { get; private set; }
            public bool IsCompleteMotionRunning { get; set; }

            public bool TryRegisterIncomingFlight(RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage || IsCompleteMotionRunning)
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

            public void Reset(RequiredPackageModel package)
            {
                Package = package;
                IncomingFlightCount = 0;
                IsCompleteMotionRunning = false;
            }
        }
    }
}
