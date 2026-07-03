namespace FoodieMatch.Runtime.Core.Infrastructure.Audio
{
    public interface IAudioService
    {
        void PlaySfx(string sfxKey);

        void PlayMusic(string musicKey);

        void StopMusic();
    }
}
