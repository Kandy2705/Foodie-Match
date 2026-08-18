using System;
using UnityEngine;

namespace FoodieMatch.UI.Profile
{
    [Serializable]
    public sealed class ProfileCustomizationEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _displayName;

        public ProfileCustomizationEntry()
        {
        }

        public ProfileCustomizationEntry(
            string id,
            Sprite sprite,
            string displayName = null)
        {
            _id = id;
            _sprite = sprite;
            _displayName = displayName;
        }

        public string Id => _id;

        public Sprite Sprite => _sprite;

        public string DisplayName => _displayName;
    }
}
