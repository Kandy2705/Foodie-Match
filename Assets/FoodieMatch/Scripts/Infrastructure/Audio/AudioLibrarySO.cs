using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Audio
{
    [CreateAssetMenu(
        fileName = "AudioLibrary",
        menuName = "FoodieMatch/Audio/Audio Library")]
    public sealed class AudioLibrarySO : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private List<AudioClipEntry> _musicClips = new();

        [Header("SFX")]
        [SerializeField] private List<AudioClipEntry> _sfxClips = new();

        public bool TryGetMusic(string key, out AudioClipEntry entry)
        {
            return TryGet(_musicClips, key, out entry);
        }

        public bool TryGetSfx(string key, out AudioClipEntry entry)
        {
            return TryGet(_sfxClips, key, out entry);
        }

        private static bool TryGet(
            List<AudioClipEntry> entries,
            string key,
            out AudioClipEntry entry)
        {
            entry = null;

            if (entries == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AudioClipEntry candidate = entries[i];

                if (candidate == null ||
                    candidate.Clip == null ||
                    string.IsNullOrEmpty(candidate.Key))
                {
                    continue;
                }

                if (string.Equals(candidate.Key, key, System.StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
