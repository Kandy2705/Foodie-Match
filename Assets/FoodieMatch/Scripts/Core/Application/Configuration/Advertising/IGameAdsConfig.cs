using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public interface IGameAdsConfig
    {
        int PostLevelAdStartLevel { get; }

        TimeSpan PostLevelAdInterval { get; }
    }
}
