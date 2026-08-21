using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Persistence.Configuration
{
    public sealed class PlayerPrefsGameCatalogCache
    {
        private const string ShopCatalogKey = "GameShopCatalog";
        private const string GoldPassCatalogKey = "GameGoldPassCatalog";

        private readonly ISaveService _saveService;

        public PlayerPrefsGameCatalogCache(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public bool TryGetShopJson(out string json)
        {
            return TryGetJson(ShopCatalogKey, out json);
        }

        public bool TryGetGoldPassJson(out string json)
        {
            return TryGetJson(GoldPassCatalogKey, out json);
        }

        public void SaveShopJson(string json)
        {
            SaveJson(ShopCatalogKey, json);
        }

        public void SaveGoldPassJson(string json)
        {
            SaveJson(GoldPassCatalogKey, json);
        }

        public void DeleteShopJson()
        {
            DeleteJson(ShopCatalogKey);
        }

        public void DeleteGoldPassJson()
        {
            DeleteJson(GoldPassCatalogKey);
        }

        private bool TryGetJson(string key, out string json)
        {
            if (!_saveService.HasKey(key))
            {
                json = null;
                return false;
            }

            json = _saveService.GetString(key, string.Empty);
            return true;
        }

        private void SaveJson(string key, string json)
        {
            _saveService.SetString(key, json);
            _saveService.Save();
        }

        private void DeleteJson(string key)
        {
            _saveService.DeleteKey(key);
            _saveService.Save();
        }
    }
}
