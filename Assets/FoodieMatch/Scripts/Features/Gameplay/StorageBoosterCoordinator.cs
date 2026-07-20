using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class StorageBoosterCoordinator
    {
        private const float DeliveryStartInterval = 0.08f;

        private readonly GameplaySessionGuard _sessionGuard;
        private readonly BoardLayoutView _boardLayoutView;
        private readonly PackageDeliveryCoordinator _packageDeliveryCoordinator;
        private readonly TopTrayMoveCoordinator _topTrayMoveCoordinator;
        private readonly Action<GameplaySession> _resolveWin;

        public StorageBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            BoardLayoutView boardLayoutView,
            PackageDeliveryCoordinator packageDeliveryCoordinator,
            TopTrayMoveCoordinator topTrayMoveCoordinator,
            Action<GameplaySession> resolveWin)
        {
            _sessionGuard = sessionGuard;
            _boardLayoutView = boardLayoutView;
            _packageDeliveryCoordinator = packageDeliveryCoordinator;
            _topTrayMoveCoordinator = topTrayMoveCoordinator;
            _resolveWin = resolveWin;
        }

        public bool TryApply(GameplaySession session)
        {
            if (!CanApply(session))
            {
                return false;
            }

            if (!TryCreatePlan(session, out StorageBoosterPlan plan))
            {
                return false;
            }

            _ = PlayMotionSafelyAsync(session, plan);
            return true;
        }

        private bool TryCreatePlan(
            GameplaySession session,
            out StorageBoosterPlan plan)
        {
            plan = default;

            if (!TryFindTargetPackage(
                    session,
                    out int packageIndex,
                    out RequiredPackageModel targetPackage))
            {
                Debug.Log("No incomplete package to fill.");
                return false;
            }

            _boardLayoutView.TryCollectActiveFoodByTokenId(
                targetPackage.FoodTokenId,
                out List<FoodItemView> grillFoodViews,
                out List<FoodBoardAddress> grillFoodAddresses);

            _boardLayoutView.TryCollectActiveFoodFromTopTrays(
                targetPackage.FoodTokenId,
                out List<FoodItemView> trayFoodViews,
                out List<int> trayGrillPositions,
                out List<int> traySlotIndices);

            if (grillFoodViews.Count == 0 && trayFoodViews.Count == 0)
            {
                Debug.Log($"No matching food found for package {packageIndex}.");
                return false;
            }

            plan = new StorageBoosterPlan(
                packageIndex,
                targetPackage,
                grillFoodViews,
                grillFoodAddresses,
                trayFoodViews,
                trayGrillPositions,
                traySlotIndices);
            return true;
        }

        private bool TryFindTargetPackage(
            GameplaySession session,
            out int packageIndex,
            out RequiredPackageModel targetPackage)
        {
            packageIndex = -1;
            targetPackage = null;
            int maxRemaining = 0;
            bool foundCompletablePackage = false;

            for (int i = 0; i < session.RequiredPackages.Length; i++)
            {
                RequiredPackageModel package = session.RequiredPackages[i];

                if (package == null || package.IsComplete)
                {
                    continue;
                }

                int remaining = package.RemainingAmount;
                int availableFoodCount = CountAvailableFood(session, package.FoodTokenId);
                bool canCompletePackage = availableFoodCount >= remaining;

                if (availableFoodCount <= 0)
                {
                    continue;
                }

                if (canCompletePackage && !foundCompletablePackage)
                {
                    foundCompletablePackage = true;
                    maxRemaining = 0;
                    packageIndex = -1;
                    targetPackage = null;
                }

                if (foundCompletablePackage && !canCompletePackage)
                {
                    continue;
                }

                if (remaining > maxRemaining)
                {
                    maxRemaining = remaining;
                    packageIndex = i;
                    targetPackage = package;
                }
            }

            return packageIndex >= 0;
        }

        private static int CountAvailableFood(
            GameplaySession session,
            int foodTokenId)
        {
            if (session?.Board == null)
            {
                return 0;
            }

            int count = 0;

            count += CountMatchingFood(
                session.Board.GetActiveFoodTokenIds(),
                foodTokenId);
            count += CountMatchingFood(
                session.Board.GetTopTrayFoodTokenIds(),
                foodTokenId);

            return count;
        }

        private static int CountMatchingFood(
            IReadOnlyList<int> foodTokenIds,
            int foodTokenId)
        {
            if (foodTokenIds == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                if (foodTokenIds[i] == foodTokenId)
                {
                    count++;
                }
            }

            return count;
        }

        private async Task PlayMotionSafelyAsync(
            GameplaySession session,
            StorageBoosterPlan plan)
        {
            try
            {
                await PlayMotionAsync(session, plan);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async Task PlayMotionAsync(
            GameplaySession session,
            StorageBoosterPlan plan)
        {
            int slotsToFill = plan.TargetPackage.RemainingAmount;
            List<Task> deliveryTasks = new();
            HashSet<int> topTrayMoveCandidates = new();
            int flightOrder = 0;

            CollectGrillFood(
                session,
                plan,
                slotsToFill,
                deliveryTasks,
                topTrayMoveCandidates,
                ref flightOrder);

            CollectTrayFood(
                session,
                plan,
                slotsToFill,
                deliveryTasks,
                topTrayMoveCandidates,
                ref flightOrder);

            MoveTopTraysToEmptyGrills(session, topTrayMoveCandidates);

            await Task.WhenAll(deliveryTasks);

            if (IsCurrentSession(session) && session.CanContinueGameplay)
            {
                _resolveWin?.Invoke(session);
            }
        }

        private void CollectGrillFood(
            GameplaySession session,
            StorageBoosterPlan plan,
            int slotsToFill,
            List<Task> deliveryTasks,
            HashSet<int> topTrayMoveCandidates,
            ref int flightOrder)
        {
            for (int i = 0; i < plan.GrillFoodViews.Count && deliveryTasks.Count < slotsToFill; i++)
            {
                FoodItemView foodView = plan.GrillFoodViews[i];

                if (foodView == null ||
                    !plan.TargetPackage.TryPlaceFood(foodView.FoodTokenId))
                {
                    continue;
                }

                _boardLayoutView.ReleaseFoodItem(foodView);

                if (i < plan.GrillFoodAddresses.Count)
                {
                    FoodBoardAddress address = plan.GrillFoodAddresses[i];

                    if (session.Board.TryRemoveFood(address, foodView.FoodTokenId))
                    {
                        topTrayMoveCandidates.Add(address.GrillPositionIndex);
                    }
                }

                deliveryTasks.Add(CreateDeliveryTask(
                    foodView,
                    plan.PackageIndex,
                    session,
                    ref flightOrder));
            }
        }

        private void CollectTrayFood(
            GameplaySession session,
            StorageBoosterPlan plan,
            int slotsToFill,
            List<Task> deliveryTasks,
            HashSet<int> topTrayMoveCandidates,
            ref int flightOrder)
        {
            for (int i = 0; i < plan.TrayFoodViews.Count && deliveryTasks.Count < slotsToFill; i++)
            {
                FoodItemView foodView = plan.TrayFoodViews[i];

                if (foodView == null ||
                    !plan.TargetPackage.TryPlaceFood(foodView.FoodTokenId))
                {
                    continue;
                }

                int grillPositionIndex = plan.TrayGrillPositions[i];
                int traySlotIndex = plan.TraySlotIndices[i];

                _boardLayoutView.ReleaseTopTrayFoodItem(
                    grillPositionIndex,
                    traySlotIndex);

                if (session.Board.TryGetGrill(
                        grillPositionIndex,
                        out GrillModel trayGrill) &&
                    trayGrill.TopTray != null)
                {
                    bool removedTrayFood = trayGrill.TopTray.TryRemoveFoodAt(
                        traySlotIndex,
                        foodView.FoodTokenId);

                    if (removedTrayFood && trayGrill.TopTray.FoodCount == 0)
                    {
                        RemoveEmptyTopTray(trayGrill, grillPositionIndex);
                        topTrayMoveCandidates.Add(grillPositionIndex);
                    }
                }

                deliveryTasks.Add(CreateDeliveryTask(
                    foodView,
                    plan.PackageIndex,
                    session,
                    ref flightOrder));
            }
        }

        private void RemoveEmptyTopTray(
            GrillModel trayGrill,
            int grillPositionIndex)
        {
            if (!trayGrill.TryRemoveEmptyTopTray() ||
                !_boardLayoutView.RemoveTopTrayVisual(trayGrill))
            {
                Debug.LogError(
                    $"Empty top tray on grill {grillPositionIndex} could not be removed.");
            }
        }

        private Task CreateDeliveryTask(
            FoodItemView foodView,
            int packageIndex,
            GameplaySession session,
            ref int flightOrder)
        {
            float startDelay = flightOrder * DeliveryStartInterval;
            flightOrder++;

            return _packageDeliveryCoordinator.DeliverSelectedFoodAsync(
                foodView,
                packageIndex,
                session,
                startDelay);
        }

        private void MoveTopTraysToEmptyGrills(
            GameplaySession session,
            HashSet<int> topTrayMoveCandidates)
        {
            foreach (int grillPositionIndex in topTrayMoveCandidates)
            {
                if (session.Board.TryGetGrill(
                        grillPositionIndex,
                        out GrillModel grill) &&
                    grill.IsEmpty)
                {
                    _topTrayMoveCoordinator.MoveFoodToGrill(grillPositionIndex, session);
                }
            }
        }

        private bool CanApply(GameplaySession session)
        {
            return session != null &&
                   session.CanContinueGameplay &&
                   session.IsInputEnabled &&
                   session.RequiredPackages != null &&
                   session.Board != null &&
                   _boardLayoutView != null &&
                   _packageDeliveryCoordinator != null &&
                   _topTrayMoveCoordinator != null &&
                   IsCurrentSession(session);
        }

        private bool IsCurrentSession(GameplaySession session)
        {
            return session != null &&
                   _sessionGuard.IsCurrentSession(session.SessionId);
        }

        private readonly struct StorageBoosterPlan
        {
            public StorageBoosterPlan(
                int packageIndex,
                RequiredPackageModel targetPackage,
                List<FoodItemView> grillFoodViews,
                List<FoodBoardAddress> grillFoodAddresses,
                List<FoodItemView> trayFoodViews,
                List<int> trayGrillPositions,
                List<int> traySlotIndices)
            {
                PackageIndex = packageIndex;
                TargetPackage = targetPackage;
                GrillFoodViews = grillFoodViews;
                GrillFoodAddresses = grillFoodAddresses;
                TrayFoodViews = trayFoodViews;
                TrayGrillPositions = trayGrillPositions;
                TraySlotIndices = traySlotIndices;
            }

            public int PackageIndex { get; }
            public RequiredPackageModel TargetPackage { get; }
            public List<FoodItemView> GrillFoodViews { get; }
            public List<FoodBoardAddress> GrillFoodAddresses { get; }
            public List<FoodItemView> TrayFoodViews { get; }
            public List<int> TrayGrillPositions { get; }
            public List<int> TraySlotIndices { get; }
        }
    }
}
