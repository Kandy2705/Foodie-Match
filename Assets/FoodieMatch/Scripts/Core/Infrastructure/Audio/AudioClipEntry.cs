using System;
using UnityEngine;

namespace FoodieMatch.Core.Infrastructure.Audio
{
    [Serializable]
    public sealed class AudioClipEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private AudioClip _clip;
        [SerializeField] [Range(0f, 1f)] private float _volume = 1f;

        public string Key => _key;

        public AudioClip Clip => _clip;

        public float Volume => _volume;
    }
}
