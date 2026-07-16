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

        public void PlayPackageEntering()
        {
            _audioService.PlaySfx(AudioKeys.SfxBoxMove);
        }

        public void PlayPackageCompleted()
        {
            _audioService.PlaySfx(AudioKeys.GetMergeComboSfx(1));
        }

        public void PlayPackageLidClosed()
        {
            _audioService.PlaySfx(AudioKeys.SfxBoxClose);
        }
    }
}
