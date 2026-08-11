using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayPointerInput : MonoBehaviour
    {
        public event Action<Vector2> PointerPressed;

        private void Update()
        {
            if (DispatchTouchPresses())
            {
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                PointerPressed?.Invoke(mouse.position.ReadValue());
                return;
            }

            Pointer pointer = Pointer.current;

            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                PointerPressed?.Invoke(pointer.position.ReadValue());
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

            return dispatched;
        }
    }
}
