using FoodieMatch.Core.Infrastructure.Save;
using UnityEngine;

namespace FoodieMatch.Core.Infrastructure.Audio
{
    public sealed class UnityAudioService : MonoBehaviour, IAudioService
    {
        private const string MusicEnabledSaveKey = "Audio.MusicEnabled";
        private const string SfxEnabledSaveKey = "Audio.SfxEnabled";
        private const int DefaultSfxSourceCount = 1;

        [Header("Library")]
        [SerializeField] private AudioLibrarySO _library;

        [Header("Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource[] _sfxSources;

        [Header("Defaults")]
        [SerializeField][Range(0f, 1f)] private float _musicVolume = 1f;
        [SerializeField][Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField] private int _sfxSourceCount = DefaultSfxSourceCount;

        private ISaveService _saveService;
        private string _currentMusicKey;
        private AudioClipEntry _currentMusicEntry;
        private int _nextSfxSourceIndex;
        private bool _isMusicEnabled = true;
        private bool _isSfxEnabled = true;

        public bool IsMusicEnabled => _isMusicEnabled;

        public bool IsSfxEnabled => _isSfxEnabled;

        private void Awake()
        {
            EnsureAudioSources();
            ApplyVolumes();
        }

        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
            EnsureAudioSources();
            LoadPreferences();
            ApplyVolumes();
        }

        public void PlaySfx(string sfxKey)
        {
            if (!_isSfxEnabled || string.IsNullOrEmpty(sfxKey))
            {
                return;
            }

            if (_library == null)
            {
                Debug.LogWarning($"{nameof(UnityAudioService)} has no audio library assigned.");
                return;
            }

            if (!_library.TryGetSfx(sfxKey, out AudioClipEntry entry) ||
                entry.Clip == null)
            {
                Debug.LogWarning($"SFX clip not found for key: {sfxKey}");
                return;
            }

            EnsureAudioSources();
            AudioSource source = GetNextSfxSource();

            if (source == null)
            {
                return;
            }

            source.PlayOneShot(entry.Clip, entry.Volume);
        }

        public void PlayMusic(string musicKey)
        {
            if (string.IsNullOrEmpty(musicKey))
            {
                return;
            }

            if (_library == null)
            {
                Debug.LogWarning($"{nameof(UnityAudioService)} has no audio library assigned.");
                return;
            }

            if (!_library.TryGetMusic(musicKey, out AudioClipEntry entry) ||
                entry.Clip == null)
            {
                Debug.LogWarning($"Music clip not found for key: {musicKey}");
                return;
            }

            EnsureAudioSources();

            if (_musicSource == null)
            {
                return;
            }

            bool isSameTrack =
                _currentMusicKey == musicKey &&
                _musicSource.clip == entry.Clip;

            _currentMusicKey = musicKey;
            _currentMusicEntry = entry;

            if (!_isMusicEnabled)
            {
                if (_musicSource.isPlaying)
                {
                    _musicSource.Stop();
                }

                return;
            }

            if (isSameTrack && _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = entry.Clip;
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume * entry.Volume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
            }

            _currentMusicKey = null;
            _currentMusicEntry = null;
        }

        public void SetMusicEnabled(bool isEnabled)
        {
            _isMusicEnabled = isEnabled;
            SavePreferences();

            if (!_isMusicEnabled)
            {
                if (_musicSource != null && _musicSource.isPlaying)
                {
                    _musicSource.Stop();
                }

                return;
            }

            if (!string.IsNullOrEmpty(_currentMusicKey))
            {
                string key = _currentMusicKey;
                _currentMusicKey = null;
                PlayMusic(key);
            }
        }

        public void SetSfxEnabled(bool isEnabled)
        {
            _isSfxEnabled = isEnabled;
            SavePreferences();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        private void LoadPreferences()
        {
            if (_saveService == null)
            {
                return;
            }

            _isMusicEnabled = _saveService.GetInt(MusicEnabledSaveKey, 1) == 1;
            _isSfxEnabled = _saveService.GetInt(SfxEnabledSaveKey, 1) == 1;
        }

        private void SavePreferences()
        {
            if (_saveService == null)
            {
                return;
            }

            _saveService.SetInt(MusicEnabledSaveKey, _isMusicEnabled ? 1 : 0);
            _saveService.SetInt(SfxEnabledSaveKey, _isSfxEnabled ? 1 : 0);
            _saveService.Save();
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                float entryVolume = _currentMusicEntry != null
                    ? _currentMusicEntry.Volume
                    : 1f;
                _musicSource.volume = _musicVolume * entryVolume;
            }

            if (_sfxSources == null)
            {
                return;
            }

            for (int i = 0; i < _sfxSources.Length; i++)
            {
                AudioSource source = _sfxSources[i];

                if (source != null)
                {
                    source.volume = _sfxVolume;
                }
            }
        }

        private AudioSource GetNextSfxSource()
        {
            if (_sfxSources == null || _sfxSources.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < _sfxSources.Length; i++)
            {
                int index = (_nextSfxSourceIndex + i) % _sfxSources.Length;
                AudioSource source = _sfxSources[index];

                if (source == null)
                {
                    continue;
                }

                if (!source.isPlaying)
                {
                    _nextSfxSourceIndex = (index + 1) % _sfxSources.Length;
                    return source;
                }
            }

            AudioSource fallback = _sfxSources[_nextSfxSourceIndex];
            _nextSfxSourceIndex = (_nextSfxSourceIndex + 1) % _sfxSources.Length;
            return fallback;
        }

        private void EnsureAudioSources()
        {
            if (_musicSource == null)
            {
                _musicSource = CreateAudioSource("MusicSource", true);
            }

            _musicSource.playOnAwake = false;
            _musicSource.loop = true;

            int desiredCount = Mathf.Max(1, _sfxSourceCount);

            if (_sfxSources == null || _sfxSources.Length != desiredCount)
            {
                AudioSource[] sources = new AudioSource[desiredCount];

                for (int i = 0; i < desiredCount; i++)
                {
                    if (_sfxSources != null &&
                        i < _sfxSources.Length &&
                        _sfxSources[i] != null)
                    {
                        sources[i] = _sfxSources[i];
                    }
                    else
                    {
                        sources[i] = CreateAudioSource($"SfxSource_{i}", false);
                    }

                    sources[i].playOnAwake = false;
                    sources[i].loop = false;
                }

                _sfxSources = sources;
            }
        }

        private AudioSource CreateAudioSource(string objectName, bool loop)
        {
            Transform existing = transform.Find(objectName);
            GameObject sourceObject;

            if (existing != null)
            {
                sourceObject = existing.gameObject;
            }
            else
            {
                sourceObject = new GameObject(objectName);
                sourceObject.transform.SetParent(transform, false);
            }

            AudioSource source = sourceObject.GetComponent<AudioSource>();

            if (source == null)
            {
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
