using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayMotionPresenter : MonoBehaviour
    {
        [Header("Top Tray Motion")]
        [SerializeField] private float _topTrayFlightStartInterval = 0.08f;

        private readonly HashSet<FoodItemView> _activeFoodMotions = new();
        private readonly HashSet<RequiredPackageView> _activePackageMotions = new();
        private readonly HashSet<TrayView> _activeTrayMotions = new();

        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;

        public float TopTrayFlightStartInterval => _topTrayFlightStartInterval;

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
            FoodItemView foodItemView,
            Vector3 targetPosition,
            float startDelay)
        {
            if (!CanStartFoodMotion(foodItemView, startDelay) ||
                !_activeFoodMotions.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await foodItemView.PlayFlightToGrillAsync(targetPosition, startDelay);
            }
            finally
            {
                _activeFoodMotions.Remove(foodItemView);
            }
        }

        public async Task<MotionResult> PlayTopTrayFadeTransitionAsync(
            TrayView departingTray,
            IReadOnlyList<FoodItemView> newTopTrayFoodItems,
            float duration)
        {
            if (departingTray == null ||
                newTopTrayFoodItems == null ||
                !IsValidTime(duration) ||
                _activeTrayMotions.Contains(departingTray))
            {
                return MotionResult.Failed;
            }

            for (int i = 0; i < newTopTrayFoodItems.Count; i++)
            {
                FoodItemView foodItemView = newTopTrayFoodItems[i];

                if (foodItemView != null && !CanStartFoodMotion(foodItemView, 0f))
                {
                    return MotionResult.Failed;
                }
            }

            List<Task<MotionResult>> motionTasks = new()
            {
                PlayTrayFadeOutAsync(departingTray, duration)
            };

            for (int i = 0; i < newTopTrayFoodItems.Count; i++)
            {
                FoodItemView foodItemView = newTopTrayFoodItems[i];

                if (foodItemView != null)
                {
                    motionTasks.Add(PlayFoodFadeInAsync(foodItemView, duration));
                }
            }

            MotionResult[] motionResults = await Task.WhenAll(motionTasks);
            return CombineMotionResults(motionResults);
        }

        public async Task<MotionResult> MoveFoodToWaitingRackAsync(
            FoodItemView foodItemView,
            int rackSlotIndex,
            float startDelay = 0f)
        {
            if (!CanStartFoodMotion(foodItemView, startDelay) ||
                _waitingRackView == null ||
                !_waitingRackView.TryReserveFoodAt(rackSlotIndex, foodItemView, out Vector3 targetPosition))
            {
                return MotionResult.Failed;
            }

            if (!_activeFoodMotions.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                MotionResult flightResult = await foodItemView.PlayFlightAsync(targetPosition, startDelay);

                if (flightResult != MotionResult.Completed)
                {
                    return flightResult;
                }

                if (_waitingRackView == null ||
                    !_waitingRackView.PrepareFoodLandingAt(rackSlotIndex, foodItemView))
                {
                    return MotionResult.Failed;
                }

                MotionResult landingResult = await foodItemView.PlayLandingFeedbackAsync();

                if (landingResult != MotionResult.Completed)
                {
                    return landingResult;
                }

                return _waitingRackView.CompleteFoodPlacementAt(rackSlotIndex, foodItemView)
                    ? MotionResult.Completed
                    : MotionResult.Failed;
            }
            finally
            {
                _activeFoodMotions.Remove(foodItemView);
            }
        }

        public async Task<MotionResult> MoveFoodToRequiredPackageAsync(
            FoodItemView foodItemView,
            int packageIndex,
            int requiredAmount,
            int filledSlotIndex,
            float startDelay = 0f)
        {
            if (!CanStartFoodMotion(foodItemView, startDelay) || _requiredPackageGroupView == null)
            {
                return MotionResult.Failed;
            }

            RequiredPackageSlotView targetSlot =
                _requiredPackageGroupView.GetTargetSlot(packageIndex, requiredAmount, filledSlotIndex);

            if (targetSlot == null)
            {
                return MotionResult.Failed;
            }

            MotionResult result = await PlayFoodFlightAndLandingAsync(
                foodItemView, targetSlot.transform, startDelay);

            if (result != MotionResult.Completed || targetSlot == null)
            {
                return result == MotionResult.Completed ? MotionResult.Failed : result;
            }

            foodItemView.Clear();
            targetSlot.SetFilled();

            return MotionResult.Completed;
        }

        public Task<MotionResult> PlayRequiredPackageMatchAsync(
            int packageIndex,
            Action<Vector3> onMatchStarted,
            Action onLidClosed)
        {
            RequiredPackageView packageView = GetAvailablePackageView(packageIndex);

            if (packageView == null)
            {
                return Task.FromResult(MotionResult.Failed);
            }

            return PlayPackageMatchAsync(packageView, onMatchStarted, onLidClosed);
        }

        public Task<MotionResult> PlayRequiredPackageEnterAsync(
            int packageIndex,
            Action onEnterStarted)
        {
            RequiredPackageView packageView = GetAvailablePackageView(packageIndex);

            if (packageView == null)
            {
                return Task.FromResult(MotionResult.Failed);
            }

            return PlayPackageEnterAsync(packageView, onEnterStarted);
        }

        public Task<MotionResult> RecenterRequiredPackagesAsync()
        {
            return _requiredPackageGroupView != null
                ? _requiredPackageGroupView.RecenterVisibleItemsAsync()
                : Task.FromResult(MotionResult.Failed);
        }

        public void CancelAllMotions()
        {
            FoodItemView[] foodItemViews = new FoodItemView[_activeFoodMotions.Count];
            _activeFoodMotions.CopyTo(foodItemViews);

            RequiredPackageView[] packageViews = new RequiredPackageView[_activePackageMotions.Count];
            _activePackageMotions.CopyTo(packageViews);

            TrayView[] trayViews = new TrayView[_activeTrayMotions.Count];
            _activeTrayMotions.CopyTo(trayViews);

            for (int i = 0; i < foodItemViews.Length; i++)
            {
                foodItemViews[i]?.CancelMotion();
            }

            for (int i = 0; i < packageViews.Length; i++)
            {
                packageViews[i]?.StopMotion();
            }

            for (int i = 0; i < trayViews.Length; i++)
            {
                trayViews[i]?.CancelMotion();
            }

            _requiredPackageGroupView?.CancelLayoutMotion();
        }

        private bool CanStartFoodMotion(FoodItemView foodItemView, float startDelay)
        {
            return foodItemView != null &&
                   !foodItemView.IsEmpty &&
                   !foodItemView.IsFlying &&
                   !_activeFoodMotions.Contains(foodItemView) &&
                   IsValidTime(startDelay);
        }

        private async Task<MotionResult> PlayFoodFadeInAsync(FoodItemView foodItemView, float duration)
        {
            if (!_activeFoodMotions.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await foodItemView.PlayFadeInAsync(duration);
            }
            finally
            {
                _activeFoodMotions.Remove(foodItemView);
            }
        }

        private async Task<MotionResult> PlayTrayFadeOutAsync(TrayView trayView, float duration)
        {
            if (!_activeTrayMotions.Add(trayView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await trayView.PlayFadeOutAsync(duration);
            }
            finally
            {
                _activeTrayMotions.Remove(trayView);
            }
        }

        private async Task<MotionResult> PlayFoodFlightAsync(
            FoodItemView foodItemView,
            Vector3 targetPosition,
            float startDelay)
        {
            if (!_activeFoodMotions.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await foodItemView.PlayFlightAsync(targetPosition, startDelay);
            }
            finally
            {
                _activeFoodMotions.Remove(foodItemView);
            }
        }

        private async Task<MotionResult> PlayFoodFlightAndLandingAsync(
            FoodItemView foodItemView,
            Transform target,
            float startDelay)
        {
            if (!_activeFoodMotions.Add(foodItemView))
            {
                return MotionResult.Failed;
            }

            try
            {
                MotionResult flightResult = await foodItemView.PlayFlightAsync(target, startDelay);

                return flightResult == MotionResult.Completed
                    ? await foodItemView.PlayLandingFeedbackAsync(target)
                    : flightResult;
            }
            finally
            {
                _activeFoodMotions.Remove(foodItemView);
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

        private async Task<MotionResult> PlayPackageMatchAsync(
            RequiredPackageView packageView,
            Action<Vector3> onMatchStarted,
            Action onLidClosed)
        {
            if (!_activePackageMotions.Add(packageView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await packageView.PlayMatchAndExitAsync(onMatchStarted, onLidClosed);
            }
            finally
            {
                _activePackageMotions.Remove(packageView);
            }
        }

        private async Task<MotionResult> PlayPackageEnterAsync(
            RequiredPackageView packageView,
            Action onEnterStarted)
        {
            if (!_activePackageMotions.Add(packageView))
            {
                return MotionResult.Failed;
            }

            try
            {
                return await packageView.PlayEnterAsync(onEnterStarted);
            }
            finally
            {
                _activePackageMotions.Remove(packageView);
            }
        }

        private static MotionResult CombineMotionResults(IReadOnlyList<MotionResult> motionResults)
        {
            bool wasCancelled = false;

            for (int i = 0; i < motionResults.Count; i++)
            {
                if (motionResults[i] == MotionResult.Failed)
                {
                    return MotionResult.Failed;
                }

                wasCancelled |= motionResults[i] == MotionResult.Cancelled;
            }

            return wasCancelled ? MotionResult.Cancelled : MotionResult.Completed;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
