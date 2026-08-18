using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    internal sealed class StackedGrillLayoutController
    {
        private const float PositionTolerance = 0.001f;

        private readonly BoardModel _board;
        private readonly List<Vector3[]> _slotPositionsByColumn = new();
        private readonly Dictionary<int, GrillMotionState> _motionStates = new();
        private readonly Dictionary<int, int> _columnByGrillPosition = new();
        private readonly HashSet<int> _removedGrillPositions = new();
        private readonly HashSet<int> _activeExitMotions = new();
        private readonly HashSet<int> _activeMoveMotions = new();

        private bool _layoutRefreshPending;

        public StackedGrillLayoutController(
            BoardModel board,
            IReadOnlyDictionary<int, GrillViewBase> grillViews)
        {
            _board = board;

            CreateMotionStates(grillViews);
            CacheColumnSlots();
        }

        public event Action MotionFinished;

        public bool HasActiveMotion =>
            _activeExitMotions.Count > 0 ||
            _activeMoveMotions.Count > 0;

        public void RefreshLayout()
        {
            HashSet<int> currentGrillPositions = GetCurrentGrillPositions();
            List<GrillMotionState> newExitMotions = new();

            foreach (KeyValuePair<int, GrillMotionState> entry in _motionStates)
            {
                if (currentGrillPositions.Contains(entry.Key) ||
                    _removedGrillPositions.Contains(entry.Key))
                {
                    continue;
                }

                _removedGrillPositions.Add(entry.Key);
                newExitMotions.Add(entry.Value);
            }

            if (newExitMotions.Count == 0)
            {
                return;
            }

            _layoutRefreshPending = true;

            for (int i = 0; i < newExitMotions.Count; i++)
            {
                GrillMotionState state = newExitMotions[i];
                StopColumnMoveMotions(_columnByGrillPosition[state.GrillPositionIndex]);
                StartExitMotion(state);
            }
        }

        public void Stop()
        {
            foreach (GrillMotionState state in _motionStates.Values)
            {
                state.StopMotions();
            }

            _activeExitMotions.Clear();
            _activeMoveMotions.Clear();
            _layoutRefreshPending = false;
        }

        private void CreateMotionStates(
            IReadOnlyDictionary<int, GrillViewBase> grillViews)
        {
            foreach (KeyValuePair<int, GrillViewBase> entry in grillViews)
            {
                _motionStates.Add(
                    entry.Key,
                    new GrillMotionState(
                        this,
                        entry.Key,
                        (StackedGrillView)entry.Value));
            }
        }

        private void CacheColumnSlots()
        {
            for (int columnIndex = 0;
                 columnIndex < _board.StackedGrillColumnCount;
                 columnIndex++)
            {
                StackedGrillColumnState column =
                    _board.GetStackedGrillColumnAt(columnIndex);
                Vector3[] slotPositions =
                    new Vector3[column.GrillPositionIndices.Count];

                for (int rowIndex = 0;
                     rowIndex < column.GrillPositionIndices.Count;
                     rowIndex++)
                {
                    int grillPositionIndex =
                        column.GrillPositionIndices[rowIndex];
                    GrillMotionState state =
                        _motionStates[grillPositionIndex];
                    slotPositions[rowIndex] =
                        state.View.transform.localPosition;
                    _columnByGrillPosition.Add(
                        grillPositionIndex,
                        columnIndex);
                }

                _slotPositionsByColumn.Add(slotPositions);
            }
        }

        private HashSet<int> GetCurrentGrillPositions()
        {
            HashSet<int> grillPositions = new();

            for (int columnIndex = 0;
                 columnIndex < _board.StackedGrillColumnCount;
                 columnIndex++)
            {
                StackedGrillColumnState column =
                    _board.GetStackedGrillColumnAt(columnIndex);

                for (int rowIndex = 0;
                     rowIndex < column.GrillPositionIndices.Count;
                     rowIndex++)
                {
                    grillPositions.Add(
                        column.GrillPositionIndices[rowIndex]);
                }
            }

            return grillPositions;
        }

        private void StartExitMotion(GrillMotionState state)
        {
            state.StopMoveMotion();
            _activeMoveMotions.Remove(state.GrillPositionIndex);
            _activeExitMotions.Add(state.GrillPositionIndex);
            state.ExitTween = state.View
                .PlayExitMotion()
                .OnComplete(
                    target: state,
                    target => target.NotifyExitCompleted());
        }

        private void CompleteExitMotion(GrillMotionState state)
        {
            state.ExitTween = default;
            state.View.gameObject.SetActive(false);
            _activeExitMotions.Remove(state.GrillPositionIndex);

            if (_activeExitMotions.Count == 0 &&
                _layoutRefreshPending)
            {
                StartSlideMotions();
            }
        }

        private void StartSlideMotions()
        {
            _layoutRefreshPending = false;

            for (int columnIndex = 0;
                 columnIndex < _board.StackedGrillColumnCount;
                 columnIndex++)
            {
                StackedGrillColumnState column =
                    _board.GetStackedGrillColumnAt(columnIndex);
                Vector3[] slotPositions =
                    _slotPositionsByColumn[columnIndex];

                for (int rowIndex = 0;
                     rowIndex < column.GrillPositionIndices.Count;
                     rowIndex++)
                {
                    int grillPositionIndex =
                        column.GrillPositionIndices[rowIndex];
                    GrillMotionState state =
                        _motionStates[grillPositionIndex];
                    StartSlideMotion(
                        state,
                        slotPositions[rowIndex]);
                }
            }

            NotifyMotionFinished();
        }

        private void StartSlideMotion(
            GrillMotionState state,
            Vector3 targetPosition)
        {
            state.StopMoveMotion();
            _activeMoveMotions.Remove(state.GrillPositionIndex);

            if (Vector3.Distance(
                    state.View.transform.localPosition,
                    targetPosition) <= PositionTolerance)
            {
                state.View.transform.localPosition = targetPosition;
                return;
            }

            _activeMoveMotions.Add(state.GrillPositionIndex);
            state.MoveTween = state.View
                .PlaySlideMotion(targetPosition)
                .OnComplete(
                    target: state,
                    target => target.NotifyMoveCompleted());
        }

        private void CompleteMoveMotion(GrillMotionState state)
        {
            state.MoveTween = default;
            _activeMoveMotions.Remove(state.GrillPositionIndex);
            NotifyMotionFinished();
        }

        private void StopColumnMoveMotions(int columnIndex)
        {
            foreach (KeyValuePair<int, int> entry in _columnByGrillPosition)
            {
                if (entry.Value != columnIndex)
                {
                    continue;
                }

                GrillMotionState state = _motionStates[entry.Key];
                state.StopMoveMotion();
                _activeMoveMotions.Remove(entry.Key);
            }
        }

        private void NotifyMotionFinished()
        {
            if (!HasActiveMotion &&
                !_layoutRefreshPending)
            {
                MotionFinished?.Invoke();
            }
        }

        private sealed class GrillMotionState
        {
            private readonly StackedGrillLayoutController _controller;

            public GrillMotionState(
                StackedGrillLayoutController controller,
                int grillPositionIndex,
                StackedGrillView view)
            {
                _controller = controller;
                GrillPositionIndex = grillPositionIndex;
                View = view;
            }

            public int GrillPositionIndex { get; }
            public StackedGrillView View { get; }
            public Tween ExitTween { get; set; }
            public Tween MoveTween { get; set; }

            public void NotifyExitCompleted()
            {
                _controller.CompleteExitMotion(this);
            }

            public void NotifyMoveCompleted()
            {
                _controller.CompleteMoveMotion(this);
            }

            public void StopMoveMotion()
            {
                if (MoveTween.isAlive)
                {
                    MoveTween.Stop();
                }

                MoveTween = default;
            }

            public void StopMotions()
            {
                if (ExitTween.isAlive)
                {
                    ExitTween.Stop();
                }

                StopMoveMotion();
                ExitTween = default;
            }
        }
    }
}
