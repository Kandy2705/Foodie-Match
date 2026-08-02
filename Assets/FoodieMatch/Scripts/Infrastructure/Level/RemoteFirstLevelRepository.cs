using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Level.Remote;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class RemoteFirstLevelRepository : ILevelRepository
    {
        private readonly ResourcesLevelRepository _resourcesRepository;
        private readonly RemoteLevelManifestLoader _manifestLoader;
        private readonly RemoteLevelPackCache _packCache;
        private readonly LevelContentJsonParser _parser;
        private readonly LevelContentValidator _validator;
        private readonly LevelContentMapper _mapper;

        public RemoteFirstLevelRepository(
            ResourcesLevelRepository resourcesRepository,
            RemoteLevelManifestLoader manifestLoader,
            RemoteLevelPackCache packCache,
            LevelContentJsonParser parser,
            LevelContentValidator validator,
            LevelContentMapper mapper)
        {
            _resourcesRepository = resourcesRepository;
            _manifestLoader = manifestLoader;
            _packCache = packCache;
            _parser = parser;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<LevelDefinition> LoadLevelAsync(int levelNumber)
        {
            if (TryLoadRemoteLevel(levelNumber, out LevelDefinition level))
            {
                return level;
            }

            return await _resourcesRepository.LoadLevelAsync(levelNumber);
        }

        private bool TryLoadRemoteLevel(
            int levelNumber,
            out LevelDefinition level)
        {
            level = null;

            if (!_manifestLoader.TryGetManifest(
                    out RemoteLevelManifestDto manifest) ||
                !TryFindPack(
                    manifest,
                    levelNumber,
                    out RemoteLevelPackDto pack) ||
                !_packCache.TryReadLevel(
                    pack,
                    levelNumber,
                    out string json,
                    out LevelSummary summary))
            {
                return false;
            }

            if (!_parser.TryParse(
                    json,
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
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors));
            }

            level = _mapper.Map(content);
            return true;
        }

        private static bool TryFindPack(
            RemoteLevelManifestDto manifest,
            int levelNumber,
            out RemoteLevelPackDto pack)
        {
            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                RemoteLevelPackDto candidate = manifest.Packs[i];

                if (levelNumber >= candidate.FirstLevel.Value &&
                    levelNumber <= candidate.LastLevel.Value)
                {
                    pack = candidate;
                    return true;
                }
            }

            pack = null;
            return false;
        }

        private static void LogWarnings(
            LevelValidationResult validationResult)
        {
            for (int i = 0;
                 i < validationResult.Warnings.Count;
                 i++)
            {
                Debug.LogWarning(validationResult.Warnings[i]);
            }
        }
    }
}
