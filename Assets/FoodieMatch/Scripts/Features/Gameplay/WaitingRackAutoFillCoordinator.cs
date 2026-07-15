using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class WaitingRackAutoFillCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly RequiredPackageLifecycleUseCase _packageLifecycleUseCase;
        private readonly WaitingRackView _waitingRackView;
        private readonly PackageDeliveryCoordinator _packageDeliveryCoordinator;

        private GameplaySession _session;
        private bool _isRunning;
        private bool _isRetryRequested;
        private bool _hasFailed;

        public event Action<GameplaySession> AutoFillFinished;
        public event Action<GameplaySession> AutoFillFailed;

        public WaitingRackAutoFillCoordinator(
            GameplaySessionGuard sessionGuard,
            RequiredPackageLifecycleUseCase packageLifecycleUseCase,
            WaitingRackView waitingRackView,
            PackageDeliveryCoordinator packageDeliveryCoordinator)
        {
            _sessionGuard = sessionGuard;
            _packageLifecycleUseCase = packageLifecycleUseCase;
            _waitingRackView = waitingRackView;
            _packageDeliveryCoordinator = packageDeliveryCoordinator;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
            _isRunning = false;
            _isRetryRequested = false;
            _hasFailed = false;
        }

        public void EndSession()
        {
            _session = null;
            _isRunning = false;
            _isRetryRequested = false;
            _hasFailed = false;
        }

        public void StartOrRequestRetry(GameplaySession session)
        {
            if (!CanContinue(session))
            {
                return;
            }

            if (_isRunning)
            {
                _isRetryRequested = true;
                return;
            }

            _isRunning = true;
            _isRetryRequested = false;
            _hasFailed = false;
            _ = RunSafelyAsync(session);
        }

        public bool IsRunning(GameplaySession session)
        {
            return _session == session && _isRunning;
        }

        private async Task RunSafelyAsync(GameplaySession session)
        {
            try
            {
                while (CanContinue(session))
                {
                    List<PackageFlight> flights = BuildBatch(session);

                    if (flights.Count == 0 || _hasFailed)
                    {
                        break;
                    }

                    if (!await _packageDeliveryCoordinator.DeliverBatchAsync(flights, session))
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Fail(session);
            }
            finally
            {
                Finish(session);
            }
        }

        private List<PackageFlight> BuildBatch(GameplaySession session)
        {
            List<PackageFlight> flights = new();

            while (CanContinue(session) &&
                   _packageLifecycleUseCase.TryFindWaitingRackMatch(
                       session.WaitingRack,
                       session.RequiredPackages,
                       out WaitingRackTransfer transfer))
            {
                FoodItemView foodItemView = _waitingRackView.RemoveFoodAt(transfer.RackSlotIndex);

                if (foodItemView == null)
                {
                    break;
                }

                if (!_packageDeliveryCoordinator.TryCreateWaitingRackFlight(
                        transfer,
                        foodItemView,
                        session,
                        out PackageFlight flight))
                {
                    Debug.LogError("Waiting rack package flight could not be created.");
                    RestoreFood(transfer.RackSlotIndex, foodItemView, session);
                    Fail(session);
                    break;
                }

                if (!_packageLifecycleUseCase.TryMoveFoodFromWaitingRack(
                        transfer,
                        session.WaitingRack,
                        session.RequiredPackages))
                {
                    Debug.LogError("Waiting rack food could not be moved to its required package.");
                    RestoreFood(transfer.RackSlotIndex, foodItemView, session);
                    break;
                }

                _packageDeliveryCoordinator.IncreaseServedFoodCount(session);
                flights.Add(flight);
            }

            return flights;
        }

        private bool RestoreFood(
            int rackSlotIndex,
            FoodItemView foodItemView,
            GameplaySession session)
        {
            if (_waitingRackView.RestoreFoodAt(rackSlotIndex, foodItemView))
            {
                return true;
            }

            Debug.LogError($"Waiting rack food at slot {rackSlotIndex} could not be restored.");
            Fail(session);
            return false;
        }

        private void Finish(GameplaySession session)
        {
            if (_session != session)
            {
                return;
            }

            bool shouldRetry = _isRetryRequested;
            bool hasFailed = _hasFailed;
            _isRunning = false;
            _isRetryRequested = false;

            if (shouldRetry && !hasFailed && CanContinue(session))
            {
                StartOrRequestRetry(session);
                return;
            }

            if (!hasFailed)
            {
                AutoFillFinished?.Invoke(session);
            }
        }

        private void Fail(GameplaySession session)
        {
            if (_session != session || _hasFailed)
            {
                return;
            }

            _hasFailed = true;
            AutoFillFailed?.Invoke(session);
        }

        private bool CanContinue(GameplaySession session)
        {
            return session != null &&
                   _session == session &&
                   _sessionGuard.IsCurrentSession(session.SessionId) &&
                   session.CanContinueGameplay;
        }
    }
}
