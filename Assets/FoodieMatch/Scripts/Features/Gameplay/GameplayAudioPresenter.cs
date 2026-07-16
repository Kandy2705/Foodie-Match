using System;
using FoodieMatch.Core.Infrastructure.Audio;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayAudioPresenter
    {
        private readonly IAudioService _audioService;

        public GameplayAudioPresenter(IAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public void PlayFoodSelected()
        {
            _audioService.PlaySfx(AudioKeys.SfxSelectSkewer);
        }

        public void PlayFoodMovedToGrill()
        {
            _audioService.PlaySfx(AudioKeys.SfxPickOff);
        }
    }
}
