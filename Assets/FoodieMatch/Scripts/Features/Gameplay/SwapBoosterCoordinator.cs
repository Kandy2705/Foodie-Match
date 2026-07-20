using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.UI;
using FoodieMatch.UI.Booster;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class SwapBoosterCoordinator
    {
        private const float HideFadeDuration = 0.25f;
        private const float HideStaggerInterval = 0.04f;
        private const float RevealFadeDuration = 0.25f;
        private const float RevealStaggerInterval = 0.04f;
        private const int MinFoodCountToSwap = 2;
        private const int GrillSlotMarker = -1;

        private readonly GameplaySessionGuard _sessionGuard;
        private readonly BoardLayoutView _boardLayoutView;
        private readonly UIManager _uiManager;
        private bool _isSwapRunning;

        public SwapBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            BoardLayoutView boardLayoutView,
            UIManager uiManager)
        {
            _sessionGuard = sessionGuard;
            _boardLayoutView = boardLayoutView;
            _uiManager = uiManager;
        }

        public bool TryApply(GameplaySession session)
        {
            if (!CanApply(session) || _isSwapRunning)
            {
                return false;
            }

            int foodCount = CountAllBoardFood(session.Board);

            if (foodCount < MinFoodCountToSwap)
            {
                return false;
            }

            _ = PlayMotionSafelyAsync(session);
            return true;
        }

        private bool CanApply(GameplaySession session)
        {
            return session != null &&
                   session.CanContinueGameplay &&
                   session.IsInputEnabled &&
                   session.Board != null &&
                   _boardLayoutView != null &&
                   _uiManager != null &&
                   IsCurrentSession(session);
        }

        private bool IsCurrentSession(GameplaySession session)
        {
            return session != null &&
                   _sessionGuard.IsCurrentSession(session.SessionId);
        }

        private async Task PlayMotionSafelyAsync(GameplaySession session)
        {
            if (_isSwapRunning)
            {
                return;
            }

            _isSwapRunning = true;

            try
            {
                await PlayMotionAsync(session);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isSwapRunning = false;
            }
        }

        private async Task PlayMotionAsync(GameplaySession session)
        {
            session.DisableInput();

            List<FoodItemView> oldFoodViews = CollectAllVisibleFoodViews();

            await _boardLayoutView.AnimateHideFoodAsync(
                oldFoodViews, HideFadeDuration, HideStaggerInterval);

            if (!IsCurrentSession(session) || !session.CanContinueGameplay)
            {
                await FinishSwapAsync(session, oldFoodViews, swapPopup: null, rebuildVisuals: false);
                return;
            }

            BoosterSwapPopup swapPopup = _uiManager.ShowSwapPopup();

            if (swapPopup != null)
            {
                await swapPopup.PlaySwapAnimationAsync();
            }

            if (!IsCurrentSession(session) || !session.CanContinueGameplay)
            {
                await FinishSwapAsync(session, oldFoodViews, swapPopup, rebuildVisuals: false);
                return;
            }

            if (!TryRearrangeAllBoardFood(session))
            {
                Debug.LogWarning("Swap booster could not rearrange board food.");
                await FinishSwapAsync(session, oldFoodViews, swapPopup, rebuildVisuals: false);
                return;
            }

            List<FoodItemView> newFoodViews = _boardLayoutView.RebuildFoodVisuals(
                session.Board,
                startHidden: true);

            if (swapPopup != null)
            {
                await swapPopup.HideAsync();
                _uiManager.HideSwapPopup();
            }

            await FinishSwapAsync(session, newFoodViews, swapPopup: null, rebuildVisuals: false);
        }

        private async Task FinishSwapAsync(
            GameplaySession session,
            List<FoodItemView> foodViews,
            BoosterSwapPopup swapPopup,
            bool rebuildVisuals)
        {
            if (swapPopup != null)
            {
                await swapPopup.HideAsync();
                _uiManager.HideSwapPopup();
            }

            if (!IsCurrentSession(session) || !session.CanContinueGameplay)
            {
                return;
            }

            if (rebuildVisuals)
            {
                foodViews = _boardLayoutView.RebuildFoodVisuals(
                    session.Board,
                    startHidden: true);
            }

            await _boardLayoutView.AnimateRevealFoodAsync(
                foodViews, RevealFadeDuration, RevealStaggerInterval);

            if (IsCurrentSession(session) && session.CanContinueGameplay)
            {
                session.StartPlaying();
            }
        }

        private List<FoodItemView> CollectAllVisibleFoodViews()
        {
            List<FoodItemView> views = new List<FoodItemView>();
            views.AddRange(_boardLayoutView.GetAllActiveFoodViews());
            views.AddRange(_boardLayoutView.GetAllTopTrayFoodViews());
            return views;
        }

        private static int CountAllBoardFood(BoardModel board)
        {
            if (board == null)
            {
                return 0;
            }

            int count = 0;

            for (int g = 0; g < board.GrillCount; g++)
            {
                GrillModel grill = board.GetGrillAt(g);

                if (grill != null)
                {
                    count += grill.RemainingFoodCount;
                }
            }

            return count;
        }

        private static bool TryRearrangeAllBoardFood(GameplaySession session)
        {
            if (session?.Board == null)
            {
                return false;
            }

            BoardModel board = session.Board;

            List<(int grillIndex, int trayIndex, int slotIndex)> grillSlots = new();
            List<(int grillIndex, int trayIndex, int slotIndex)> traySlots = new();

            List<int> grillTokens = new();
            List<int> trayTokens = new();

            for (int g = 0; g < board.GrillCount; g++)
            {
                GrillModel grill = board.GetGrillAt(g);
                if (grill == null) continue;

                for (int s = 0; s < grill.ActiveFoodSlotCount; s++)
                {
                    int tokenId = grill.GetFoodTokenIdAt(s);

                    if (tokenId > BoardRules.EmptyFoodTokenId)
                    {
                        grillSlots.Add((g, GrillSlotMarker, s));
                        grillTokens.Add(tokenId);
                    }
                }

                for (int t = 0; t < grill.TrayCount; t++)
                {
                    TrayModel tray = grill.GetTrayAt(t);
                    if (tray == null) continue;

                    for (int s = 0; s < tray.SlotCount; s++)
                    {
                        int tokenId = tray.GetFoodTokenIdAt(s);

                        if (tokenId > BoardRules.EmptyFoodTokenId)
                        {
                            traySlots.Add((g, t, s));
                            trayTokens.Add(tokenId);
                        }
                    }
                }
            }

            List<int> allTokens = new List<int>(
                grillTokens.Count + trayTokens.Count);

            allTokens.AddRange(grillTokens);
            allTokens.AddRange(trayTokens);

            if (allTokens.Count < MinFoodCountToSwap)
            {
                return false;
            }

            List<(int grillIndex, int trayIndex, int slotIndex)> allSlots =
                new List<(int grillIndex, int trayIndex, int slotIndex)>(
                    grillSlots.Count + traySlots.Count);

            allSlots.AddRange(grillSlots);
            allSlots.AddRange(traySlots);

            RequiredPackageModel[] packages = session.RequiredPackages;

            List<int> reorderedTokens = ReorderForMergeSupport(
                allTokens,
                packages,
                grillSlots.Count);

            if (reorderedTokens.Count != allSlots.Count)
            {
                return false;
            }

            List<int> originalTokens = new List<int>(allTokens);

            ClearOccupiedSlots(board, allSlots);

            for (int i = 0; i < allSlots.Count; i++)
            {
                (int g, int t, int s) = allSlots[i];

                if (TryWriteToken(board, g, t, s, reorderedTokens[i]))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"Could not write swap token at grill={g}, tray={t}, slot={s}.");

                ClearOccupiedSlots(board, allSlots);

                for (int restoreIndex = 0;
                     restoreIndex < allSlots.Count;
                     restoreIndex++)
                {
                    (int restoreG, int restoreT, int restoreS) =
                        allSlots[restoreIndex];

                    TryWriteToken(
                        board,
                        restoreG,
                        restoreT,
                        restoreS,
                        originalTokens[restoreIndex]);
                }

                return false;
            }

            RemoveEmptyTopTrays(board);
            return true;
        }
        private static void ClearOccupiedSlots(
            BoardModel board,
            List<(int grillIndex, int trayIndex, int slotIndex)> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                (int g, int t, int s) = slots[i];
                TryWriteToken(board, g, t, s, BoardRules.EmptyFoodTokenId);
            }
        }

        private static bool TryWriteToken(
            BoardModel board,
            int grillIndex,
            int trayIndex,
            int slotIndex,
            int tokenId)
        {
            if (!board.TryGetGrill(grillIndex, out GrillModel grill) || grill == null)
            {
                return false;
            }

            if (trayIndex == GrillSlotMarker)
            {
                return grill.TrySetFoodTokenIdAt(slotIndex, tokenId);
            }

            TrayModel tray = grill.GetTrayAt(trayIndex);
            return tray != null && tray.TrySetFoodTokenIdAt(slotIndex, tokenId);
        }

        private static void RemoveEmptyTopTrays(BoardModel board)
        {
            for (int i = 0; i < board.GrillCount; i++)
            {
                GrillModel grill = board.GetGrillAt(i);
                if (grill == null) continue;

                while (grill.TopTray != null && grill.TopTray.FoodCount == 0)
                {
                    if (!grill.TryRemoveEmptyTopTray())
                    {
                        break;
                    }
                }
            }
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<int> ReorderForMergeSupport(
        List<int> tokenIds,
        RequiredPackageModel[] packages,
        int grillSlotCount)
        {
            if (tokenIds == null || tokenIds.Count == 0)
            {
                return new List<int>();
            }

            grillSlotCount = Math.Min(
                Math.Max(0, grillSlotCount),
                tokenIds.Count);

            Dictionary<int, int> remainingCounts =
                new Dictionary<int, int>();

            for (int i = 0; i < tokenIds.Count; i++)
            {
                int tokenId = tokenIds[i];

                if (!remainingCounts.ContainsKey(tokenId))
                {
                    remainingCounts[tokenId] = 0;
                }

                remainingCounts[tokenId]++;
            }

            Dictionary<int, int> packageNeed =
                BuildPackageNeed(packages);

            List<int> preferredTypes = new List<int>();

            foreach (KeyValuePair<int, int> pair in packageNeed)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (remainingCounts.TryGetValue(
                        pair.Key,
                        out int availableCount) &&
                    availableCount > 0)
                {
                    preferredTypes.Add(pair.Key);
                }
            }

            Dictionary<int, int> randomPriority =
                new Dictionary<int, int>();

            for (int i = 0; i < preferredTypes.Count; i++)
            {
                randomPriority[preferredTypes[i]] =
                    UnityEngine.Random.Range(0, int.MaxValue);
            }

            preferredTypes.Sort((left, right) =>
            {
                int leftUsefulCount = Math.Min(
                    remainingCounts[left],
                    packageNeed[left]);

                int rightUsefulCount = Math.Min(
                    remainingCounts[right],
                    packageNeed[right]);

                int usefulCompare =
                    rightUsefulCount.CompareTo(leftUsefulCount);

                if (usefulCompare != 0)
                {
                    return usefulCompare;
                }

                return randomPriority[left]
                    .CompareTo(randomPriority[right]);
            });

            Dictionary<int, int> usefulRemaining =
                new Dictionary<int, int>();

            for (int i = 0; i < preferredTypes.Count; i++)
            {
                int tokenId = preferredTypes[i];

                usefulRemaining[tokenId] = Math.Min(
                    remainingCounts[tokenId],
                    packageNeed[tokenId]);
            }

            List<int> grillResult =
                new List<int>(grillSlotCount);

            bool madeProgress = true;

            while (grillResult.Count < grillSlotCount &&
                   madeProgress)
            {
                madeProgress = false;

                for (int i = 0; i < preferredTypes.Count; i++)
                {
                    if (grillResult.Count >= grillSlotCount)
                    {
                        break;
                    }

                    int tokenId = preferredTypes[i];

                    if (!usefulRemaining.TryGetValue(
                            tokenId,
                            out int usefulCount) ||
                        usefulCount <= 0)
                    {
                        continue;
                    }

                    grillResult.Add(tokenId);

                    usefulRemaining[tokenId] = usefulCount - 1;
                    remainingCounts[tokenId]--;

                    madeProgress = true;
                }
            }

            List<int> remainingTokens = new List<int>();

            foreach (KeyValuePair<int, int> pair in remainingCounts)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    remainingTokens.Add(pair.Key);
                }
            }

            Shuffle(remainingTokens);

            while (grillResult.Count < grillSlotCount &&
                   remainingTokens.Count > 0)
            {
                int lastIndex = remainingTokens.Count - 1;

                grillResult.Add(remainingTokens[lastIndex]);
                remainingTokens.RemoveAt(lastIndex);
            }

            Shuffle(grillResult);

            Shuffle(remainingTokens);

            List<int> result =
                new List<int>(tokenIds.Count);

            result.AddRange(grillResult);
            result.AddRange(remainingTokens);

            return result;
        }

        private static Dictionary<int, int> BuildPackageNeed(
            RequiredPackageModel[] packages)
        {
            Dictionary<int, int> need = new Dictionary<int, int>();

            if (packages == null)
            {
                return need;
            }

            for (int i = 0; i < packages.Length; i++)
            {
                RequiredPackageModel package = packages[i];

                if (package == null || package.IsComplete || package.IsEmpty)
                {
                    continue;
                }

                int tokenId = package.FoodTokenId;
                int remaining = package.RemainingAmount;

                if (!need.ContainsKey(tokenId))
                {
                    need[tokenId] = 0;
                }

                need[tokenId] += remaining;
            }

            return need;
        }

        private static int ResolveSlotsPerGrill(BoardModel board)
        {
            if (board == null)
            {
                return 0;
            }

            for (int g = 0; g < board.GrillCount; g++)
            {
                GrillModel grill = board.GetGrillAt(g);

                if (grill != null && grill.ActiveFoodSlotCount > 0)
                {
                    return grill.ActiveFoodSlotCount;
                }
            }

            return 0;
        }

        private static void EnsureOrderChanged(
            List<int> originalTokenIds,
            List<int> reorderedTokenIds)
        {
            if (originalTokenIds == null ||
                reorderedTokenIds == null ||
                reorderedTokenIds.Count < 2 ||
                originalTokenIds.Count != reorderedTokenIds.Count)
            {
                return;
            }

            bool isSame = true;

            for (int i = 0; i < reorderedTokenIds.Count; i++)
            {
                if (originalTokenIds[i] != reorderedTokenIds[i])
                {
                    isSame = false;
                    break;
                }
            }

            if (!isSame)
            {
                return;
            }

            int first = reorderedTokenIds[0];
            reorderedTokenIds.RemoveAt(0);
            reorderedTokenIds.Add(first);
        }
    }
}
