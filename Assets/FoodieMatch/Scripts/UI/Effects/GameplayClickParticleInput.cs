using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FoodieMatch.UI.Effects
{
    public sealed class GameplayClickParticleInput : MonoBehaviour
    {
        private Action<Vector2> _pointerPressed;
        private bool _isInputEnabled;

        public void Construct(Action<Vector2> pointerPressed)
        {
            _pointerPressed = pointerPressed;
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            _isInputEnabled = inputEnabled;
        }

        private void Update()
        {
            if (!_isInputEnabled)
            {
                return;
            }

            if (PlayTouchEffects())
            {
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                _pointerPressed(mouse.position.ReadValue());
                return;
            }

            Pointer pointer = Pointer.current;

            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                _pointerPressed(pointer.position.ReadValue());
            }
        }

        private bool PlayTouchEffects()
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen == null)
            {
                return false;
            }

            bool playedEffect = false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                _pointerPressed(touch.position.ReadValue());
                playedEffect = true;
            }

            return playedEffect;
        }
    }
}
