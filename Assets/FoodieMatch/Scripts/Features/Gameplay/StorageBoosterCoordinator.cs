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
        private readonly Action<int, GameplaySession> _refillGrill;
        private readonly Action<GameplaySession> _resolveWin;

        public StorageBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            BoardLayoutView boardLayoutView,
            PackageDeliveryCoordinator packageDeliveryCoordinator,
            Action<int, GameplaySession> refillGrill,
            Action<GameplaySession> resolveWin)
        {
            _sessionGuard = sessionGuard;
            _boardLayoutView = boardLayoutView;
            _packageDeliveryCoordinator = packageDeliveryCoordinator;
            _refillGrill = refillGrill;
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

            CollectAccessibleGrillFood(
                session,
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

        private void CollectAccessibleGrillFood(
            GameplaySession session,
            int foodTokenId,
            out List<FoodItemView> foodViews,
            out List<FoodBoardAddress> foodAddresses)
        {
            foodViews = new();
            foodAddresses = new();
            List<FoodBoardEntry> entries = _boardLayoutView.GetActiveFoodEntries();

            for (int i = 0; i < entries.Count; i++)
            {
                FoodBoardEntry entry = entries[i];

                if (entry.FoodItemView.FoodTokenId != foodTokenId ||
                    !session.Board.IsGrillInActiveRows(
                        entry.Address.GrillPositionIndex))
                {
                    continue;
                }

                foodViews.Add(entry.FoodItemView);
                foodAddresses.Add(entry.Address);
            }
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

            return packageIndex >= 0 && foundCompletablePackage;
        }

        private static int CountAvailableFood(
            GameplaySession session,
            int foodTokenId)
        {
            int count = 0;

            for (int grillIndex = 0; grillIndex < session.Board.GrillCount; grillIndex++)
            {
                GrillModel grill = session.Board.GetGrillAt(grillIndex);

                if (!session.Board.IsGrillInActiveRows(grill.PositionIndex))
                {
                    continue;
                }

                count += CountMatchingFood(
                    grill.ActiveFoodSlotCount,
                    grill.GetFoodTokenIdAt,
                    foodTokenId);

                if (grill.Type == GrillType.Standard && grill.TopTray != null)
                {
                    count += CountMatchingFood(
                        grill.TopTray.SlotCount,
                        grill.TopTray.GetFoodTokenIdAt,
                        foodTokenId);
                }
            }

            return count;
        }

        private static int CountMatchingFood(
            int foodSlotCount,
            Func<int, int> getFoodTokenIdAt,
            int foodTokenId)
        {
            int count = 0;

            for (int i = 0; i < foodSlotCount; i++)
            {
                if (getFoodTokenIdAt(i) == foodTokenId)
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
            HashSet<int> grillRefillCandidates = new();
            int flightOrder = 0;

            CollectGrillFood(
                session,
                plan,
                slotsToFill,
                deliveryTasks,
                grillRefillCandidates,
                ref flightOrder);

            CollectTrayFood(
                session,
                plan,
                slotsToFill,
                deliveryTasks,
                grillRefillCandidates,
                ref flightOrder);

            RefillEmptyGrills(session, grillRefillCandidates);

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
            HashSet<int> grillRefillCandidates,
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
                        session.Board.TryRemoveEmptyGrillFromColumn(
                            address.GrillPositionIndex);
                        grillRefillCandidates.Add(address.GrillPositionIndex);
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
            HashSet<int> grillRefillCandidates,
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
                        grillRefillCandidates.Add(grillPositionIndex);
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

        private void RefillEmptyGrills(
            GameplaySession session,
            HashSet<int> grillPositionIndices)
        {
            foreach (int grillPositionIndex in grillPositionIndices)
            {
                if (session.Board.TryGetGrill(
                        grillPositionIndex,
                        out GrillModel grill) &&
                    grill.IsEmpty)
                {
                    _refillGrill(grillPositionIndex, session);
                }
            }
        }

        private bool CanApply(GameplaySession session)
        {
            return session != null &&
                   session.CanContinueGameplay &&
                   session.IsInputEnabled &&
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
