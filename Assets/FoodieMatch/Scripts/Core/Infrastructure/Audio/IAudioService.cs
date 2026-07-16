namespace FoodieMatch.Core.Infrastructure.Audio
{
    public interface IAudioService
    {
        bool IsMusicEnabled { get; }

        bool IsSfxEnabled { get; }

        void PlaySfx(string sfxKey);

        void PlayMusic(string musicKey);

        void StopMusic();

        void SetMusicEnabled(bool isEnabled);

        void SetSfxEnabled(bool isEnabled);

        void SetMusicVolume(float volume);

        void SetSfxVolume(float volume);
    }
}
