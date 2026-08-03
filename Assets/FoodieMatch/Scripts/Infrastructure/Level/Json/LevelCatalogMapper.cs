using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelCatalogMapper
    {
        public ResourcesLevelCatalogData Map(LevelCatalogDto catalogDto)
        {
            if (catalogDto == null)
            {
                throw new ArgumentNullException(nameof(catalogDto));
            }

            Dictionary<int, LevelCatalogEntryDto> entriesById = new();
            Dictionary<int, string> contentFiles = new();

            for (int i = 0; i < catalogDto.Levels.Count; i++)
            {
                LevelCatalogEntryDto entry = catalogDto.Levels[i];
                entriesById.Add(entry.Id.Value, entry);
                contentFiles.Add(entry.Id.Value, entry.ContentFile);
            }

            List<LevelSummary> orderedLevels = new();

            for (int i = 0; i < catalogDto.LevelOrder.Count; i++)
            {
                int levelId = catalogDto.LevelOrder[i];
                LevelCatalogEntryDto entry = entriesById[levelId];

                Enum.TryParse(
                    entry.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty);
                orderedLevels.Add(new LevelSummary(levelId, difficulty));
            }

            LevelCatalog catalog = new(orderedLevels);
            return new ResourcesLevelCatalogData(catalog, contentFiles);
        }
    }
}
