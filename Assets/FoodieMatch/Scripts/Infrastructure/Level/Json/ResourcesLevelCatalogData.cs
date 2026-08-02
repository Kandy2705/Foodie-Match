using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class ResourcesLevelCatalogData
    {
        private readonly ReadOnlyDictionary<int, string> _contentFiles;

        public ResourcesLevelCatalogData(
            LevelCatalog catalog,
            IReadOnlyDictionary<int, string> contentFiles)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            if (contentFiles == null)
            {
                throw new ArgumentNullException(nameof(contentFiles));
            }

            Dictionary<int, string> copiedContentFiles = new(contentFiles);
            _contentFiles = new ReadOnlyDictionary<int, string>(copiedContentFiles);
        }

        public LevelCatalog Catalog { get; }

        public IReadOnlyDictionary<int, string> ContentFiles => _contentFiles;
    }
}
