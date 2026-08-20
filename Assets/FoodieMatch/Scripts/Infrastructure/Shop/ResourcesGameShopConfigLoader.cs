using FoodieMatch.Core.Application.Configuration.Shop;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Shop
{
    public sealed class ResourcesGameShopConfigLoader
    {
        private const string ResourcePath = "Shop/shop_bundles";
        private readonly GameShopConfigJsonParser _parser = new();

        public bool TryLoad(out IGameShopConfig config, out string errorMessage)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                config = null;
                errorMessage = $"Shop config resource {ResourcePath} was not found.";
                return false;
            }

            return _parser.TryParse(
                asset.text,
                out config,
                out errorMessage);
        }
    }
}
