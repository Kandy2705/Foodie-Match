using System.Collections.Generic;
using FoodieMatch.Core.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FoodieMatch.UI.Common
{
    public sealed class UiGlobalButtonClickSfx : MonoBehaviour
    {
        private readonly List<RaycastResult> _raycastResults = new(16);

        private IAudioService _audioService;

        public void Construct(IAudioService audioService)
        {
            _audioService = audioService;
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
                return;
            }

            GameObject hitObject = _raycastResults[0].gameObject;

            if (hitObject == null)
            {
                return;
            }

            Button button = hitObject.GetComponentInParent<Button>();

            if (button != null)
            {
                if (IsSelectableClickable(button))
                {
                    _audioService.PlaySfx(AudioKeys.SfxClick);
                }

                return;
            }

            Toggle toggle = hitObject.GetComponentInParent<Toggle>();

            if (toggle != null && IsSelectableClickable(toggle))
            {
                _audioService.PlaySfx(AudioKeys.SfxClick);
            }
        }

        private static bool IsSelectableClickable(Selectable selectable)
        {
            return selectable != null &&
                   selectable.isActiveAndEnabled &&
                   selectable.IsInteractable();
        }

        private static bool TryGetPrimaryPressScreenPosition(out Vector2 screenPosition)
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
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}
