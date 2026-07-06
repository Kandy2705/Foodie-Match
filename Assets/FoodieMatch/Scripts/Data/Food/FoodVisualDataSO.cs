using UnityEngine;

namespace FoodieMatch.Data.Food
{
    [CreateAssetMenu(
        fileName = "FoodVisualData",
        menuName = "FoodieMatch/Food/Food Visual Data")]
    public sealed class FoodVisualDataSO : ScriptableObject
    {
        [SerializeField] private string _visualId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        public string VisualId => _visualId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
    }
}
