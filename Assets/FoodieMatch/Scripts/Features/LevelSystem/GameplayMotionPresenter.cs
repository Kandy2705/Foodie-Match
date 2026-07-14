using System;
using System.Collections.Generic;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.LevelSystem
{
    public sealed class GameplayMotionPresenter : MonoBehaviour
    {
        [SerializeField] private float _foodFlightDuration = 0.22f;

        private readonly Dictionary<FoodItemView, FoodFlight> _activeFlights =
            new Dictionary<FoodItemView, FoodFlight>();

        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;

        private void OnDestroy()
        {
            CancelAllFlights();
        }

        public void Construct(
            RequiredPackageGroupView requiredPackageGroupView,
            WaitingRackView waitingRackView)
        {
            _requiredPackageGroupView = requiredPackageGroupView;
            _waitingRackView = waitingRackView;
        }

        public bool TryMoveFoodToWaitingRack(
            FoodItemView foodItemView,
            int rackSlotIndex,
            Action onFlightCompleted)
        {
            if (!CanStartFlight(foodItemView) ||
                _waitingRackView == null ||
                !_waitingRackView.TryReserveFoodAt(
                    rackSlotIndex,
                    foodItemView,
                    out Vector3 targetPosition))
            {
                return false;
            }

            FoodFlight flight = FoodFlight.ToWaitingRack(
                rackSlotIndex);

            _waitingRackView.WhenPlacementCompleted(
                rackSlotIndex,
                onFlightCompleted);

            StartFlight(
                foodItemView,
                targetPosition,
                flight);

            return true;
        }

        public bool TryMoveFoodToRequiredPackage(
            FoodItemView foodItemView,
            int packageIndex,
            int requiredAmount,
            int filledSlotIndex,
            Action onFlightCompleted)
        {
            if (!CanStartFlight(foodItemView) ||
                _requiredPackageGroupView == null)
            {
                return false;
            }

            RequiredPackageSlotView targetSlot =
                _requiredPackageGroupView.GetTargetSlot(
                    packageIndex,
                    requiredAmount,
                    filledSlotIndex);

            if (targetSlot == null)
            {
                return false;
            }

            FoodFlight flight = FoodFlight.ToRequiredPackage(
                targetSlot,
                onFlightCompleted);

            StartFlight(
                foodItemView,
                targetSlot.WorldPosition,
                flight);

            return true;
        }

        public void CancelAllFlights()
        {
            List<FoodItemView> foodItemViews =
                new List<FoodItemView>(_activeFlights.Keys);

            _activeFlights.Clear();

            for (int i = 0; i < foodItemViews.Count; i++)
            {
                if (foodItemViews[i] != null)
                {
                    foodItemViews[i].CancelMotion();
                }
            }
        }

        private bool CanStartFlight(FoodItemView foodItemView)
        {
            return foodItemView != null &&
                   !foodItemView.IsEmpty &&
                   !foodItemView.IsFlying &&
                   !_activeFlights.ContainsKey(foodItemView) &&
                   _foodFlightDuration >= 0f;
        }

        private void StartFlight(
            FoodItemView foodItemView,
            Vector3 targetPosition,
            FoodFlight flight)
        {
            _activeFlights.Add(foodItemView, flight);

            if (foodItemView.TryPlayFlight(
                    targetPosition,
                    _foodFlightDuration,
                    OnFoodFlightCompleted))
            {
                return;
            }

            _activeFlights.Remove(foodItemView);
            FinishFlight(foodItemView, flight);
        }

        private void OnFoodFlightCompleted(FoodItemView foodItemView)
        {
            if (foodItemView == null ||
                !_activeFlights.TryGetValue(
                    foodItemView,
                    out FoodFlight flight))
            {
                return;
            }

            _activeFlights.Remove(foodItemView);
            FinishFlight(foodItemView, flight);
        }

        private void FinishFlight(
            FoodItemView foodItemView,
            FoodFlight flight)
        {
            if (flight.Target == FoodFlightTarget.WaitingRack)
            {
                _waitingRackView?.CompleteFoodPlacementAt(
                    flight.RackSlotIndex,
                    foodItemView);
            }
            else
            {
                foodItemView.Clear();

                if (flight.PackageSlot != null)
                {
                    flight.PackageSlot.SetFilled();
                    flight.PackageSlot.PlayLandingFeedback();
                }
            }

            flight.OnFlightCompleted?.Invoke();
        }

        private enum FoodFlightTarget
        {
            WaitingRack,
            RequiredPackage
        }

        private readonly struct FoodFlight
        {
            private FoodFlight(
                FoodFlightTarget target,
                int rackSlotIndex,
                RequiredPackageSlotView packageSlot,
                Action onFlightCompleted)
            {
                Target = target;
                RackSlotIndex = rackSlotIndex;
                PackageSlot = packageSlot;
                OnFlightCompleted = onFlightCompleted;
            }

            public FoodFlightTarget Target { get; }
            public int RackSlotIndex { get; }
            public RequiredPackageSlotView PackageSlot { get; }
            public Action OnFlightCompleted { get; }

            public static FoodFlight ToWaitingRack(
                int rackSlotIndex)
            {
                return new FoodFlight(
                    FoodFlightTarget.WaitingRack,
                    rackSlotIndex,
                    null,
                    null);
            }

            public static FoodFlight ToRequiredPackage(
                RequiredPackageSlotView packageSlot,
                Action onFlightCompleted)
            {
                return new FoodFlight(
                    FoodFlightTarget.RequiredPackage,
                    -1,
                    packageSlot,
                    onFlightCompleted);
            }
        }
    }
}
