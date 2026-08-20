using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayPointerInput : MonoBehaviour
    {
        private readonly List<RaycastResult> _raycastResults = new(16);

        private EventSystem _eventSystem;
        private PointerEventData _pointerEventData;
        private Func<Vector2, bool> _worldInputBlockCheck;

        public event Action<Vector2> PointerPressed;
        public event Action<GameplayPointerPress> PrimaryPointerPressed;

        public void Construct(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
            _pointerEventData = new PointerEventData(eventSystem);
        }

        public void SetWorldInputBlockCheck(
            Func<Vector2, bool> worldInputBlockCheck)
        {
            _worldInputBlockCheck = worldInputBlockCheck;
        }

        private void Update()
        {
            if (DispatchTouchPresses())
            {
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                DispatchPrimaryPointerPress(mouse.position.ReadValue());
                return;
            }

            Pointer pointer = Pointer.current;

            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                DispatchPrimaryPointerPress(pointer.position.ReadValue());
            }
        }

        private bool DispatchTouchPresses()
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen == null)
            {
                return false;
            }

            bool dispatched = false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                PointerPressed?.Invoke(touch.position.ReadValue());
                dispatched = true;
            }

            if (touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                DispatchPrimaryPress(
                    touchscreen.primaryTouch.position.ReadValue());
            }

            return dispatched;
        }

        private void DispatchPrimaryPointerPress(Vector2 screenPosition)
        {
            PointerPressed?.Invoke(screenPosition);
            DispatchPrimaryPress(screenPosition);
        }

        private void DispatchPrimaryPress(Vector2 screenPosition)
        {
            Action<GameplayPointerPress> primaryPointerPressed =
                PrimaryPointerPressed;

            if (primaryPointerPressed == null)
            {
                return;
            }

            primaryPointerPressed.Invoke(
                new GameplayPointerPress(
                    screenPosition,
                    IsPointerOverUi(screenPosition)));
        }

        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            if (_worldInputBlockCheck?.Invoke(screenPosition) == true)
            {
                return true;
            }

            _pointerEventData.position = screenPosition;
            _raycastResults.Clear();
            _eventSystem.RaycastAll(_pointerEventData, _raycastResults);

            foreach (RaycastResult result in _raycastResults)
            {
                if (result.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }
    }
        public readonly struct GameplayPointerPress
    {
        public GameplayPointerPress(
            Vector2 screenPosition,
            bool isOverUi)
        {
            ScreenPosition = screenPosition;
            IsOverUi = isOverUi;
        }

        public Vector2 ScreenPosition { get; }
        public bool IsOverUi { get; }
    }

}
