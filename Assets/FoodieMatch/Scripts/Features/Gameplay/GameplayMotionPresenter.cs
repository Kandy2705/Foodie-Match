using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayMotionPresenter : MonoBehaviour
    {
        [SerializeField] private float _foodFlightDuration = 0.22f;

        private readonly HashSet<FoodItemView> _activeFoodFlights = new();
        private readonly HashSet<RequiredPackageView> _activePackageMotions = new();

        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;

        private void OnDestroy()
        {
            CancelAllMotions();
        }

        public void Construct(RequiredPackageGroupView requiredPackageGroupView, WaitingRackView waitingRackView)
        {
            _requiredPackageGroupView = requiredPackageGroupView;
            _waitingRackView = waitingRackView;
        }

        public async Task<MotionResult> MoveTopTrayFoodToGrillAsync(
            IReadOnlyList<FoodItemView> foodItemViews,
            IReadOnlyList<Vector3> targetPositions)
        {
            if (foodItemViews == null ||
                targetPositions == null ||
                foodItemViews.Count != targetPositions.Count)
            {
                return MotionResult.Failed;
            }

            for (int i = 0; i < foodItemViews.Count; i++)
            {
                FoodItemView foodItemView = foodItemViews[i];

                if (foodItemView != null && !CanStartFoodFlight(foodItemView, 0f))
                {
                    return MotionResult.Failed;
                }
            }

            List<Task<MotionResult>> motionTasks = new();

            for (int i = 0; i < foodItemViews.Count; i++)
            {
                FoodItemView foodItemView = foodItemViews[i];

                if (foodItemView == null)
                {
                    continue;
                }

                motionTasks.Add(PlayFoodFlightAsync(foodItemView, targetPositions[i], 0f));
            }

            MotionResult[] motionResults = await Task.WhenAll(motionTasks);
            bool wasCancelled = false;

            for (int i = 0; i < motionResults.Length; i++)
            {
                if (motionResults[i] == MotionResult.Failed)
                {
                    return MotionResult.Failed;
                }

                wasCancelled |= motionResults[i] == MotionResult.Cancelled;
            }

            return wasCancelled ? MotionResult.Cancelled : MotionResult.Completed;
        }

        public async Task<MotionResult> MoveFoodToWaitingRackAsync(
            FoodItemView foodItemView,
            int rackSlotIndex,
            float startDelay = 0f)
        {
            if (!CanStartFoodFlight(foodItemView, startDelay) ||
                _waitingRackView == null ||
                !_waitingRackView.TryReserveFoodAt(rackSlotIndex, foodItemView, out Vector3 targetPosition))
            {
                return MotionResult.Failed;
            }

            MotionResult result = await PlayFoodFlightAsync(foodItemView, targetPosition, startDelay);

            if (result != MotionResult.Completed)
            {
                return result;
            }

            if (_waitingRackView == null)
            {
                return MotionResult.Failed;
            }

            return _waitingRackView.CompleteFoodPlacementAt(rackSlotIndex, foodItemView)
                ? MotionResult.Completed
                : MotionResult.Failed;
        }

        public async Task<MotionResult> MoveFoodToRequiredPackageAsync(
            FoodItemView foodItemView,
            int packageIndex,
            int requiredAmount,
            int filledSlotIndex,
            float startDelay = 0f)
        {
            if (!CanStartFoodFlight(foodItemView, startDelay) || _requiredPackageGroupView == null)
            {
                return MotionResult.Failed;
            }

            RequiredPackageSlotView targetSlot =
                _requiredPackageGroupView.GetTargetSlot(packageIndex, requiredAmount, filledSlotIndex);

            if (targetSlot == null)
            {
                return MotionResult.Failed;
            }

            MotionResult result = await PlayFoodFlightAsync(foodItemView, targetSlot.transform, startDelay);

            if (result != MotionResult.Completed || targetSlot == null)
            {
                return result == MotionResult.Completed ? MotionResult.Failed : result;
            }

            foodItemView.Clear();
            targetSlot.SetFilled();
            targetSlot.PlayLandingFeedback();

            return MotionResult.Completed;
        }

        public Task<MotionResult> PlayRequiredPackageMatchAsync(int packageIndex)
        {
            RequiredPackageView packageView = GetAvailablePackageView(packageIndex);

            if (packageView == null)
            {
                return Task.FromResult(MotionResult.Failed);
            }

            return PlayPackageMatchAsync(packageView);
        }

        public Task<MotionResult> PlayRequiredPackageEnterAsync(int packageIndex)
        {
            RequiredPackageView packageView = GetAvailablePackageView(packageIndex);

            if (packageView == null)
            {
                return Task.FromResult(MotionResult.Failed);
            }

            return PlayPackageEnterAsync(packageView);
        }

        public Task<MotionResult> RecenterRequiredPackagesAsync()
        {
            return _requiredPackageGroupView != null
                ? _requiredPackageGroupView.RecenterVisibleItemsAsync()
                : Task.FromResult(MotionResult.Failed);
        }

        public void CancelAllMotions()
        {
            FoodItemView[] foodItemViews = new FoodItemView[_activeFoodFlights.Count];
            _activeFoodFlights.CopyTo(foodItemViews);

            RequiredPackageView[] packageViews = new RequiredPackageView[_activePackageMotions.Count];
            _activePackageMotions.CopyTo(packageViews);

            for (int i = 0; i < foodItemViews.Length; i++)
            {
                foodItemViews[i]?.StopFlight();
            }

            for (int i = 0; i < packageViews.Length; i++)
            {
                packageViews[i]?.StopMotion();
            }

            _requiredPackageGroupView?.CancelLayoutMotion();
        }

        private bool CanStartFoodFlight(FoodItemView foodItemView, float startDelay)
        {
            return foodItemView != null &&
                   !foodItemView.IsEmpty &&
                   !foodItemView.IsFlying &&
                   !_activeFoodFlights.Contains(foodItemView) &&
                   IsValidTime(_foodFlightDuration) &&
                   IsValidTime(startDelay);
        }

        private async Task<MotionResult> PlayFoodFlightAsync(
            FoodItemView foodItemView,
            Vector3 targetPosition,
            float startDelay)
        {
            if (!_activeFoodFlights.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await foodItemView.PlayFlightAsync(targetPosition, _foodFlightDuration, startDelay);
            }
            finally
            {
                _activeFoodFlights.Remove(foodItemView);
            }
        }

        private async Task<MotionResult> PlayFoodFlightAsync(
            FoodItemView foodItemView,
            Transform target,
            float startDelay)
        {
            if (!_activeFoodFlights.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await foodItemView.PlayFlightAsync(target, _foodFlightDuration, startDelay);
            }
            finally
            {
                _activeFoodFlights.Remove(foodItemView);
            }
        }

        private RequiredPackageView GetAvailablePackageView(int packageIndex)
        {
            if (_requiredPackageGroupView == null)
            {
                return null;
            }

            RequiredPackageView packageView = _requiredPackageGroupView.GetPackageAt(packageIndex);
            return packageView != null && !_activePackageMotions.Contains(packageView) ? packageView : null;
        }

        private async Task<MotionResult> PlayPackageMatchAsync(RequiredPackageView packageView)
        {
            if (!_activePackageMotions.Add(packageView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await packageView.PlayMatchAndExitAsync();
            }
            finally
            {
                _activePackageMotions.Remove(packageView);
            }
        }

        private async Task<MotionResult> PlayPackageEnterAsync(RequiredPackageView packageView)
        {
            if (!_activePackageMotions.Add(packageView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await packageView.PlayEnterAsync();
            }
            finally
            {
                _activePackageMotions.Remove(packageView);
            }
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
