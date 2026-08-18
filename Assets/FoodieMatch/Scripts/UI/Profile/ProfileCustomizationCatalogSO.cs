using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.UI.Profile
{
    [CreateAssetMenu(
        fileName = "ProfileCustomizationCatalog",
        menuName = "FoodieMatch/Profile/Profile Customization Catalog")]
    public sealed class ProfileCustomizationCatalogSO : ScriptableObject
    {
        private const string FallbackDefaultAvatarId = "avatar_01";
        private const string FallbackDefaultFrameId = "frame_01";

        [SerializeField] private List<ProfileCustomizationEntry> _avatars = new();
        [SerializeField] private List<ProfileCustomizationEntry> _frames = new();

        public IReadOnlyList<ProfileCustomizationEntry> Avatars => _avatars;

        public IReadOnlyList<ProfileCustomizationEntry> Frames => _frames;

        public string DefaultAvatarId => _avatars != null && _avatars.Count > 0
            ? _avatars[0].Id
            : FallbackDefaultAvatarId;

        public string DefaultFrameId => _frames != null && _frames.Count > 0
            ? _frames[0].Id
            : FallbackDefaultFrameId;

        public bool TryGetAvatar(string id, out ProfileCustomizationEntry entry)
        {
            if (_avatars == null || string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _avatars.Count; i++)
            {
                ProfileCustomizationEntry candidate = _avatars[i];
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public bool TryGetFrame(string id, out ProfileCustomizationEntry entry)
        {
            if (_frames == null || string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _frames.Count; i++)
            {
                ProfileCustomizationEntry candidate = _frames[i];
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public Sprite GetAvatarSpriteOrDefault(string id)
        {
            if (TryGetAvatar(id, out ProfileCustomizationEntry entry) && entry.Sprite != null)
            {
                return entry.Sprite;
            }

            if (_avatars != null && _avatars.Count > 0)
            {
                for (int i = 0; i < _avatars.Count; i++)
                {
                    if (_avatars[i] != null && _avatars[i].Sprite != null)
                    {
                        return _avatars[i].Sprite;
                    }
                }
            }

            return null;
        }

        public Sprite GetFrameSpriteOrDefault(string id)
        {
            if (TryGetFrame(id, out ProfileCustomizationEntry entry) && entry.Sprite != null)
            {
                return entry.Sprite;
            }

            if (_frames != null && _frames.Count > 0)
            {
                for (int i = 0; i < _frames.Count; i++)
                {
                    if (_frames[i] != null && _frames[i].Sprite != null)
                    {
                        return _frames[i].Sprite;
                    }
                }
            }

            return null;
        }
    }
}
