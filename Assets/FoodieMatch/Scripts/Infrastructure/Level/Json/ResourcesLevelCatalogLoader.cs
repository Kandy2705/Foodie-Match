using System;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class ResourcesLevelCatalogLoader
    {
        private const string CatalogResourcePath = "Data/Levels/level_catalog";

        private readonly LevelCatalogJsonParser _catalogParser;
        private readonly LevelCatalogValidator _catalogValidator;
        private readonly LevelCatalogMapper _mapper;

        public ResourcesLevelCatalogLoader(
            LevelCatalogJsonParser catalogParser,
            LevelCatalogValidator catalogValidator,
            LevelCatalogMapper mapper)
        {
            _catalogParser = catalogParser ??
                             throw new ArgumentNullException(nameof(catalogParser));
            _catalogValidator = catalogValidator ??
                                throw new ArgumentNullException(nameof(catalogValidator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public bool TryLoad(
            out ResourcesLevelCatalogData catalogData,
            out LevelValidationResult validationResult)
        {
            catalogData = null;
            validationResult = new LevelValidationResult();

            TextAsset catalogAsset = Resources.Load<TextAsset>(CatalogResourcePath);

            if (catalogAsset == null)
            {
                validationResult.AddError(
                    $"Level catalog resource '{CatalogResourcePath}' could not be found.");
                return false;
            }

            LevelCatalogDto catalogDto;

            try
            {
                if (!_catalogParser.TryParse(
                        catalogAsset.text,
                        out catalogDto,
                        out string parseError))
                {
                    validationResult.AddError(parseError);
                    return false;
                }
            }
            finally
            {
                Resources.UnloadAsset(catalogAsset);
            }

            validationResult = _catalogValidator.Validate(catalogDto);

            if (!validationResult.IsValid)
            {
                return false;
            }

            try
            {
                catalogData = _mapper.Map(catalogDto);
                return true;
            }
            catch (ArgumentException exception)
            {
                validationResult.AddError(
                    $"Level catalog could not be mapped: {exception.Message}");
                return false;
            }
        }
    }
}
