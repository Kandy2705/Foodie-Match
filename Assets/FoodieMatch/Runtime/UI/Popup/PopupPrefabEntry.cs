using System;
using UnityEngine;
namespace FoodieMatch.Runtime.UI.Popup
{
    [Serializable]
    public sealed class PopupPrefabEntry
    {
        [SerializeField] private PopupBase _prefab;
        [SerializeField] private bool _cacheAfterHide;

        public PopupBase Prefab => _prefab;
        public bool CacheAfterHide => _cacheAfterHide;
    }
}
