namespace FoodieMatch.Runtime.Core.Infrastructure.Save
{
    public interface ISaveService
    {
        void SetInt(string key, int value);

        int GetInt(string key, int defaultValue);

        void SetString(string key, string value);

        string GetString(string key, string defaultValue);

        bool HasKey(string key);

        void DeleteKey(string key);

        void Save();
    }
}
