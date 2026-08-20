using FoodieMatch.Core.Application.Configuration.GoldPass;
using UnityEngine;

namespace FoodieMatch.Infrastructure.GoldPass
{
    public sealed class ResourcesGameGoldPassConfigLoader
    {
        private const string ResourcePath = "GoldPass/gold_pass";
        private readonly GameGoldPassConfigJsonParser _parser = new();

        public bool TryLoad(
            out IGameGoldPassConfig config,
            out string errorMessage)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                config = null;
                errorMessage =
                    $"Gold Pass config resource {ResourcePath} was not found.";
                return false;
            }

            return _parser.TryParse(
                asset.text,
                out config,
                out errorMessage);
        }
    }
}
