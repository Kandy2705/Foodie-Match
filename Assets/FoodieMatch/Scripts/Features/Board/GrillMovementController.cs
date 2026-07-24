using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    internal sealed class GrillMovementController
    {
        private readonly Camera _worldCamera;
        private readonly List<MovingGrillGroup> _groups = new();

        private bool _isRunning;

        public GrillMovementController(
            Camera worldCamera,
            BoardModel board,
            IReadOnlyList<GrillMovementGroupDefinition>
                groupDefinitions,
            IReadOnlyDictionary<int, GrillView> grillViews)
        {
            _worldCamera = worldCamera ??
                           throw new ArgumentNullException(
                               nameof(worldCamera));

            if (!_worldCamera.orthographic)
            {
                throw new ArgumentException(
                    "Grill movement camera must be orthographic.",
                    nameof(worldCamera));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (groupDefinitions == null)
            {
                throw new ArgumentNullException(
                    nameof(groupDefinitions));
            }

            if (grillViews == null)
            {
                throw new ArgumentNullException(nameof(grillViews));
            }

            CreateGroups(
                board,
                groupDefinitions,
                grillViews);
        }

        public void StartMovement()
        {
            _isRunning = _groups.Count > 0;
        }

        public void StopMovement()
        {
            _isRunning = false;
        }

        public void Advance(float deltaTime)
        {
            if (!_isRunning ||
                !IsValidPositiveNumber(deltaTime) ||
                !TryGetCameraBounds(out Rect cameraBounds))
            {
                return;
            }

            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].Advance(
                    deltaTime,
                    cameraBounds);
            }
        }

        private void CreateGroups(
            BoardModel board,
            IReadOnlyList<GrillMovementGroupDefinition>
                groupDefinitions,
            IReadOnlyDictionary<int, GrillView> grillViews)
        {
            for (int groupIndex = 0;
                 groupIndex < groupDefinitions.Count;
                 groupIndex++)
            {
                GrillMovementGroupDefinition definition =
                    groupDefinitions[groupIndex];
                List<GrillView> groupGrillViews =
                    new(definition.GrillIds.Count);

                for (int grillIndex = 0;
                     grillIndex < definition.GrillIds.Count;
                     grillIndex++)
                {
                    int grillId =
                        definition.GrillIds[grillIndex];

                    if (!board.TryGetGrillById(
                            grillId,
                            out GrillModel grillModel) ||
                        !grillViews.TryGetValue(
                            grillModel.PositionIndex,
                            out GrillView grillView) ||
                        grillView == null)
                    {
                        throw new InvalidOperationException(
                            $"Grill movement group references missing grill {grillId}.");
                    }

                    groupGrillViews.Add(grillView);
                }

                _groups.Add(
                    new MovingGrillGroup(
                        definition.Direction,
                        definition.MovementSpeed,
                        groupGrillViews));
            }
        }

        private bool TryGetCameraBounds(out Rect bounds)
        {
            bounds = default;

            if (_worldCamera == null ||
                !_worldCamera.orthographic)
            {
                return false;
            }

            Vector3 bottomLeft =
                _worldCamera.ViewportToWorldPoint(
                    new Vector3(0f, 0f, 0f));
            Vector3 topRight =
                _worldCamera.ViewportToWorldPoint(
                    new Vector3(1f, 1f, 0f));

            float width = topRight.x - bottomLeft.x;
            float height = topRight.y - bottomLeft.y;

            if (!IsValidPositiveNumber(width) ||
                !IsValidPositiveNumber(height))
            {
                return false;
            }

            bounds = new Rect(
                bottomLeft.x,
                bottomLeft.y,
                width,
                height);
            return true;
        }

        private static bool IsValidPositiveNumber(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}
