using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    internal sealed class MovingGrillGroup
    {
        private readonly List<MovingGrill> _grills;
        private readonly GrillMovementDirection _direction;
        private readonly float _movementSpeed;
        private readonly float _spacing;
        private readonly float _maximumVisualSize;

        public MovingGrillGroup(
            GrillMovementDirection direction,
            float movementSpeed,
            IReadOnlyList<GrillView> grillViews)
        {
            if (!Enum.IsDefined(typeof(GrillMovementDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            if (!IsValidPositiveNumber(movementSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            if (grillViews == null)
            {
                throw new ArgumentNullException(nameof(grillViews));
            }

            if (grillViews.Count < 2)
            {
                throw new ArgumentException(
                    "Moving grill group must contain at least two grill views.",
                    nameof(grillViews));
            }

            _direction = direction;
            _movementSpeed = movementSpeed;
            _grills = CreateMovingGrills(grillViews);
            _grills.Sort(CompareGrills);
            _spacing = CalculateSpacing(_grills);
            _maximumVisualSize = CalculateMaximumVisualSize(_grills);
        }

        public void Advance(float deltaTime, Rect cameraBounds)
        {
            if (!IsValidPositiveNumber(deltaTime) ||
                cameraBounds.width <= 0f ||
                cameraBounds.height <= 0f)
            {
                return;
            }

            Vector3 movement = GetMovementVector() * (_movementSpeed * deltaTime);

            for (int i = 0; i < _grills.Count; i++)
            {
                MovingGrill grill = _grills[i];

                if (grill.View != null)
                {
                    grill.View.transform.position += movement;
                }
            }

            float cameraSpan = IsHorizontal()
                ? cameraBounds.width
                : cameraBounds.height;
            float loopDistance = CalculateLoopDistance(cameraSpan);

            for (int i = 0; i < _grills.Count; i++)
            {
                WrapGrill(_grills[i], cameraBounds, loopDistance);
            }
        }

        private List<MovingGrill> CreateMovingGrills(
            IReadOnlyList<GrillView> grillViews)
        {
            List<MovingGrill> grills = new(grillViews.Count);

            for (int i = 0; i < grillViews.Count; i++)
            {
                GrillView grillView = grillViews[i];

                if (grillView == null)
                {
                    throw new ArgumentException(
                        "Moving grill view is missing.",
                        nameof(grillViews));
                }

                grills.Add(CreateMovingGrill(grillView));
            }

            return grills;
        }

        private MovingGrill CreateMovingGrill(GrillView grillView)
        {
            Transform grillTransform = grillView.transform;
            float grillPosition = GetAxisPosition(grillTransform.position);
            Renderer[] renderers =
                grillView.GetComponentsInChildren<Renderer>(
                    includeInactive: true);

            if (!TryGetRendererBounds(renderers, out Bounds bounds))
            {
                return new MovingGrill(
                    grillView,
                    minimumVisualOffset: 0f,
                    maximumVisualOffset: 0f);
            }

            float minimumVisualOffset =
                GetAxisPosition(bounds.min) - grillPosition;
            float maximumVisualOffset =
                GetAxisPosition(bounds.max) - grillPosition;

            return new MovingGrill(
                grillView,
                minimumVisualOffset,
                maximumVisualOffset);
        }

        private int CompareGrills(MovingGrill left, MovingGrill right)
        {
            return GetAxisPosition(left.View.transform.position)
                .CompareTo(
                    GetAxisPosition(
                        right.View.transform.position));
        }

        private float CalculateSpacing(
            IReadOnlyList<MovingGrill> grills)
        {
            float firstPosition =
                GetAxisPosition(
                    grills[0].View.transform.position);
            float secondPosition =
                GetAxisPosition(
                    grills[1].View.transform.position);
            float spacing = Mathf.Abs(secondPosition - firstPosition);

            if (!IsValidPositiveNumber(spacing))
            {
                throw new ArgumentException(
                    "Moving grill spacing must be greater than zero.",
                    nameof(grills));
            }

            return spacing;
        }

        private static float CalculateMaximumVisualSize(
            IReadOnlyList<MovingGrill> grills)
        {
            float maximumVisualSize = 0f;

            for (int i = 0; i < grills.Count; i++)
            {
                MovingGrill grill = grills[i];
                float visualSize =
                    grill.MaximumVisualOffset -
                    grill.MinimumVisualOffset;
                maximumVisualSize =
                    Mathf.Max(maximumVisualSize, visualSize);
            }

            return maximumVisualSize;
        }

        private float CalculateLoopDistance(float cameraSpan)
        {
            int visibleSlotCount = Mathf.CeilToInt(
                (cameraSpan + _maximumVisualSize) /
                _spacing);
            int loopSlotCount = Mathf.Max(
                _grills.Count,
                visibleSlotCount);

            return loopSlotCount * _spacing;
        }

        private void WrapGrill(
            MovingGrill grill,
            Rect cameraBounds,
            float loopDistance)
        {
            if (grill.View == null ||
                !IsValidPositiveNumber(loopDistance))
            {
                return;
            }

            Transform grillTransform = grill.View.transform;
            Vector3 position = grillTransform.position;

            if (IsMovingPositive())
            {
                float maximumBoundary = IsHorizontal()
                    ? cameraBounds.xMax
                    : cameraBounds.yMax;

                while (GetAxisPosition(position) +
                       grill.MinimumVisualOffset >
                       maximumBoundary)
                {
                    position = MoveAlongAxis(
                        position,
                        -loopDistance);
                }
            }
            else
            {
                float minimumBoundary = IsHorizontal()
                    ? cameraBounds.xMin
                    : cameraBounds.yMin;

                while (GetAxisPosition(position) +
                       grill.MaximumVisualOffset <
                       minimumBoundary)
                {
                    position = MoveAlongAxis(
                        position,
                        loopDistance);
                }
            }

            grillTransform.position = position;
        }

        private Vector3 GetMovementVector()
        {
            return _direction switch
            {
                GrillMovementDirection.Left => Vector3.left,
                GrillMovementDirection.Right => Vector3.right,
                GrillMovementDirection.Up => Vector3.up,
                GrillMovementDirection.Down => Vector3.down,
                _ => Vector3.zero
            };
        }

        private bool IsHorizontal()
        {
            return _direction is
                GrillMovementDirection.Left or
                GrillMovementDirection.Right;
        }

        private bool IsMovingPositive()
        {
            return _direction is
                GrillMovementDirection.Right or
                GrillMovementDirection.Up;
        }

        private float GetAxisPosition(Vector3 position)
        {
            return IsHorizontal()
                ? position.x
                : position.y;
        }

        private Vector3 MoveAlongAxis(
            Vector3 position,
            float distance)
        {
            if (IsHorizontal())
            {
                position.x += distance;
            }
            else
            {
                position.y += distance;
            }

            return position;
        }

        private static bool TryGetRendererBounds(
            IReadOnlyList<Renderer> renderers,
            out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static bool IsValidPositiveNumber(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private readonly struct MovingGrill
        {
            public MovingGrill(
                GrillView view,
                float minimumVisualOffset,
                float maximumVisualOffset)
            {
                View = view;
                MinimumVisualOffset = minimumVisualOffset;
                MaximumVisualOffset = maximumVisualOffset;
            }

            public GrillView View { get; }
            public float MinimumVisualOffset { get; }
            public float MaximumVisualOffset { get; }
        }
    }
}
