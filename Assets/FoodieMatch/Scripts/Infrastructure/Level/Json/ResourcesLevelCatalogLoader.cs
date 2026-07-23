using System;
using FoodieMatch.Core.Domain.Level;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class ResourcesLevelCatalogLoader
    {
        private const string CatalogResourcePath = "Data/Levels/level_catalog";

        private readonly LevelCatalogJsonParser _parser;
        private readonly LevelCatalogValidator _validator;
        private readonly LevelCatalogMapper _mapper;

        public ResourcesLevelCatalogLoader(
            LevelCatalogJsonParser parser,
            LevelCatalogValidator validator,
            LevelCatalogMapper mapper)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
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

            if (!_parser.TryParse(
                    catalogAsset.text,
                    out LevelCatalogDto catalogDto,
                    out string parseError))
            {
                validationResult.AddError(parseError);
                return false;
            }

            validationResult = _validator.Validate(catalogDto);

            if (!validationResult.IsValid)
            {
                return false;
            }

            try
            {
                catalog = _mapper.Map(catalogDto);
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
