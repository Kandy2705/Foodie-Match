using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level.Json;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class ResourcesLevelRepository : ILevelRepository
    {
        private const string ContentResourcePath =
            "Data/Levels/Content";

        private readonly ILevelCatalogRepository _catalogRepository;
        private readonly IReadOnlyDictionary<int, string> _contentFiles;
        private readonly LevelContentJsonParser _parser;
        private readonly LevelContentValidator _validator;
        private readonly LevelContentMapper _mapper;

        public ResourcesLevelRepository(
            ILevelCatalogRepository catalogRepository,
            IReadOnlyDictionary<int, string> contentFiles,
            LevelContentJsonParser parser,
            LevelContentValidator validator,
            LevelContentMapper mapper)
        {
            _catalogRepository = catalogRepository ??
                                throw new ArgumentNullException(nameof(catalogRepository));
            _contentFiles = contentFiles ??
                            throw new ArgumentNullException(nameof(contentFiles));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _validator = validator ??
                         throw new ArgumentNullException(nameof(validator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<LevelDefinition> LoadLevelAsync(int levelNumber)
        {
            if (!_catalogRepository.TryGetLevelSummary(
                    levelNumber,
                    out LevelSummary summary))
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} is not listed in the level catalog.");
            }

            string contentFile = _contentFiles[levelNumber];
            string resourcePath = $"{ContentResourcePath}/{contentFile}";
            TextAsset contentAsset = await LoadTextAssetAsync(resourcePath);

            if (contentAsset == null)
            {
                throw new InvalidOperationException(
                    $"Level content resource '{resourcePath}' could not be found.");
            }

            try
            {
                if (!_parser.TryParse(
                        contentAsset.text,
                        out LevelContentDto content,
                        out string parseError))
                {
                    throw new InvalidOperationException(parseError);
                }

                LevelValidationResult validationResult = new();
                _validator.Validate(content, summary, validationResult);
                LogWarnings(validationResult);

                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(
                        string.Join(Environment.NewLine, validationResult.Errors));
                }

                return _mapper.Map(content);
            }
            finally
            {
                Resources.UnloadAsset(contentAsset);
            }
        }

        private static async Task<TextAsset> LoadTextAssetAsync(
            string resourcePath)
        {
            ResourceRequest request =
                Resources.LoadAsync<TextAsset>(resourcePath);

            while (!request.isDone)
            {
                await Task.Yield();
            }

            return request.asset as TextAsset;
        }

        private static void LogWarnings(
            LevelValidationResult validationResult)
        {
            for (int i = 0; i < validationResult.Warnings.Count; i++)
            {
                Debug.LogWarning(validationResult.Warnings[i]);
            }
        }
    }
}
