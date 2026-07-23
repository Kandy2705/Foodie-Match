using System.Collections.Generic;
using FoodieMatch.Core.Application.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayWorldClickSfx : MonoBehaviour
    {
        private readonly List<RaycastResult> _raycastResults = new(16);

        private IAudioService _audioService;

        public void Construct(IAudioService audioService)
        {
            _audioService = audioService;
            enabled = false;
        }

        public void StartListening()
        {
            enabled = true;
        }

        public void StopListening()
        {
            enabled = false;
        }

        private void Update()
        {
            if (_audioService == null || !_audioService.IsSfxEnabled)
            {
                return;
            }

            if (!TryGetPrimaryPressScreenPosition(out Vector2 screenPosition))
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return;
            }

            PointerEventData pointerEventData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };

            _raycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, _raycastResults);

            if (_raycastResults.Count == 0)
            {
                _audioService.PlaySfx(AudioKeys.SfxScreenTap);
                return;
            }

            GameObject hitObject = _raycastResults[0].gameObject;

            if (hitObject == null)
            {
                _audioService.PlaySfx(AudioKeys.SfxScreenTap);
                return;
            }

            if (IsUiButtonOrToggle(hitObject))
            {
                return;
            }

            if (IsUiGraphicHit(hitObject))
            {
                return;
            }

            _audioService.PlaySfx(AudioKeys.SfxScreenTap);
        }

        private static bool IsUiButtonOrToggle(GameObject hitObject)
        {
            Button button = hitObject.GetComponentInParent<Button>();

            if (button != null &&
                button.isActiveAndEnabled &&
                button.IsInteractable())
            {
                return true;
            }

            Toggle toggle = hitObject.GetComponentInParent<Toggle>();

            return toggle != null &&
                   toggle.isActiveAndEnabled &&
                   toggle.IsInteractable();
        }

        private static bool IsUiGraphicHit(GameObject hitObject)
        {
            return hitObject.GetComponentInParent<Graphic>() != null &&
                   hitObject.GetComponentInParent<Canvas>() != null;
        }

        private static bool TryGetPrimaryPressScreenPosition(
            out Vector2 screenPosition)
        {
            screenPosition = default;

            if (Pointer.current != null &&
                Pointer.current.press.wasPressedThisFrame)
            {
                screenPosition = Pointer.current.position.ReadValue();
                return true;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition =
                    Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}
