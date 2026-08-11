using FoodieMatch.Core.Application.Audio;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayWorldClickSfx : MonoBehaviour
    {
        private IAudioService _audioService;

        public void Construct(
            IAudioService audioService,
            GameplayPointerInput pointerInput)
        {
            _audioService = audioService;
            pointerInput.PrimaryPointerPressed += OnPrimaryPointerPressed;
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

        private void OnPrimaryPointerPressed(GameplayPointerPress pointerPress)
        {
            if (!isActiveAndEnabled ||
                !_audioService.IsSfxEnabled ||
                pointerPress.IsOverUi)
            {
                return;
            }

            _audioService.PlaySfx(AudioKeys.SfxScreenTap);
        }
    }
}
