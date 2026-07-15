namespace FoodieMatch.Core.Infrastructure.Audio
{
    public sealed class NullAudioService : IAudioService
    {
        public bool IsMusicEnabled { get; private set; } = true;

        public bool IsSfxEnabled { get; private set; } = true;

        public void PlaySfx(string sfxKey)
        {
        }

        public void PlayMusic(string musicKey)
        {
        }

        public void StopMusic()
        {
        }

        public void SetMusicEnabled(bool isEnabled)
        {
            IsMusicEnabled = isEnabled;
        }

        public void SetSfxEnabled(bool isEnabled)
        {
            IsSfxEnabled = isEnabled;
        }

        public void SetMusicVolume(float volume)
        {
        }

        public void SetSfxVolume(float volume)
        {
        }
    }
}
