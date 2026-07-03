using UnityEngine;
namespace FoodieMatch.Runtime.Core.Infrastructure.Audio
{
    public sealed class NullAudioService : IAudioService
    {
        public void PlaySfx(string sfxKey)
        {
        }

        public void PlayMusic(string musicKey)
        {
        }

        public void StopMusic()
        {
        }
    }
}
