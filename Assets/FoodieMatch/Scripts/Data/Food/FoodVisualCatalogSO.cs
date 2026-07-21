using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Food
{
    [CreateAssetMenu(
        fileName = "FoodVisualCatalog",
        menuName = "FoodieMatch/Food/Food Visual Catalog")]
    public sealed class FoodVisualCatalogSO : ScriptableObject
    {
        [SerializeField] private List<Sprite> _icons = new();

        public IReadOnlyList<Sprite> Icons => _icons;
    }
}
