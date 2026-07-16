using System.Collections.Generic;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
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

            if (IsSelectableGameplayTarget(hitObject))
            {
                _audioService.PlaySfx(AudioKeys.SfxSelectSkewer);
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

        private static bool IsSelectableGameplayTarget(GameObject hitObject)
        {
            FoodItemView foodItemView =
                hitObject.GetComponentInParent<FoodItemView>();

            if (foodItemView != null &&
                !foodItemView.IsEmpty &&
                foodItemView.IsInteractable)
            {
                return true;
            }

            LockedRequiredPackageView lockedPackage =
                hitObject.GetComponentInParent<LockedRequiredPackageView>();

            return lockedPackage != null && lockedPackage.IsInteractable;
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
