using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class ResourcesLevelCatalogLoader
    {
        private const string CatalogResourcePath = "Data/Levels/level_catalog";
        private const string ContentResourcePath = "Data/Levels/Content";

        private readonly LevelCatalogJsonParser _catalogParser;
        private readonly LevelContentJsonParser _contentParser;
        private readonly LevelCatalogValidator _catalogValidator;
        private readonly LevelContentValidator _contentValidator;
        private readonly LevelCatalogMapper _mapper;

        public ResourcesLevelCatalogLoader(
            LevelCatalogJsonParser catalogParser,
            LevelContentJsonParser contentParser,
            LevelCatalogValidator catalogValidator,
            LevelContentValidator contentValidator,
            LevelCatalogMapper mapper)
        {
            _catalogParser = catalogParser ??
                             throw new ArgumentNullException(nameof(catalogParser));
            _contentParser = contentParser ??
                             throw new ArgumentNullException(nameof(contentParser));
            _catalogValidator = catalogValidator ??
                                throw new ArgumentNullException(nameof(catalogValidator));
            _contentValidator = contentValidator ??
                                throw new ArgumentNullException(nameof(contentValidator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public bool TryLoad(
            out LevelCatalog catalog,
            out LevelValidationResult validationResult)
        {
            catalog = null;
            validationResult = new LevelValidationResult();

            TextAsset catalogAsset = Resources.Load<TextAsset>(CatalogResourcePath);

            if (catalogAsset == null)
            {
                validationResult.AddError(
                    $"Level catalog resource '{CatalogResourcePath}' could not be found.");
                return false;
            }

            if (!_catalogParser.TryParse(
                    catalogAsset.text,
                    out LevelCatalogDto catalogDto,
                    out string parseError))
            {
                validationResult.AddError(parseError);
                return false;
            }

            validationResult = _catalogValidator.Validate(catalogDto);

            if (!validationResult.IsValid)
            {
                return false;
            }

            Dictionary<int, LevelDto> levelsById =
                LoadLevelContents(catalogDto, validationResult);

            if (!validationResult.IsValid)
            {
                return false;
            }

            try
            {
                catalog = _mapper.Map(catalogDto, levelsById);
                return true;
            }
            catch (ArgumentException exception)
            {
                validationResult.AddError(
                    $"Level catalog could not be mapped: {exception.Message}");
                return false;
            }
        }

        private Dictionary<int, LevelDto> LoadLevelContents(
            LevelCatalogDto catalogDto,
            LevelValidationResult validationResult)
        {
            Dictionary<int, LevelDto> levelsById = new();

            for (int i = 0; i < catalogDto.Levels.Count; i++)
            {
                LevelCatalogEntryDto catalogEntry = catalogDto.Levels[i];
                string resourcePath =
                    $"{ContentResourcePath}/{catalogEntry.ContentFile}";
                TextAsset contentAsset = Resources.Load<TextAsset>(resourcePath);

                if (contentAsset == null)
                {
                    validationResult.AddError(
                        $"Level content resource '{resourcePath}' could not be found.");
                    continue;
                }

                if (!_contentParser.TryParse(
                        contentAsset.text,
                        out LevelContentDto content,
                        out string parseError))
                {
                    validationResult.AddError(
                        $"Level content '{catalogEntry.ContentFile}' is invalid: {parseError}");
                    continue;
                }

                _contentValidator.Validate(
                    content,
                    catalogEntry,
                    i,
                    validationResult);

                if (content.Level != null && catalogEntry.Id.HasValue)
                {
                    levelsById.Add(catalogEntry.Id.Value, content.Level);
                }
            }

            return levelsById;
        }
    }
}
