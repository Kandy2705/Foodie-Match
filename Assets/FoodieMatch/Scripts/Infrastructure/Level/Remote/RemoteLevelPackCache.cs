using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level.Json;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelPackCache
    {
        private const string LocalManifestFileName = "pack_manifest.json";

        private static readonly Encoding FileEncoding = new UTF8Encoding(false, true);

        private readonly LevelDiskCache _diskCache;
        private readonly LevelContentJsonParser _levelParser;
        private readonly LevelContentValidator _levelValidator;
        private readonly RemoteLevelPackManifestParser _manifestParser = new();
        private readonly RemoteLevelPackManifestValidator _manifestValidator = new();

        public RemoteLevelPackCache(
            LevelDiskCache diskCache,
            LevelContentJsonParser levelParser,
            LevelContentValidator levelValidator)
        {
            _diskCache = diskCache;
            _levelParser = levelParser;
            _levelValidator = levelValidator;
        }

        public bool IsAvailable(RemoteLevelPackDto expectedPack)
        {
            string packDirectory = GetVersionDirectory(expectedPack);
            string manifestPath = $"{packDirectory}/{LocalManifestFileName}";

            if (!_diskCache.TryReadBytes(
                    manifestPath,
                    out byte[] manifestContent) ||
                !TryParseManifest(
                    manifestContent,
                    expectedPack,
                    out RemoteLevelPackManifestDto manifest))
            {
                return false;
            }

            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                RemoteLevelEntryDto level = manifest.Levels[i];

                if (!_diskCache.TryReadBytes(
                        $"{packDirectory}/{level.ContentPath}",
                        out byte[] levelContent) ||
                    !RemoteLevelFileHash.Matches(
                        levelContent,
                        level.Sha256))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ContainsLevel(
            RemoteLevelPackDto expectedPack,
            int levelNumber)
        {
            return TryFindAvailablePack(
                expectedPack,
                out RemoteLevelPackManifestDto manifest,
                out _) &&
                   FindLevel(manifest, levelNumber) != null;
        }

        public bool TryReadLevel(
            RemoteLevelPackDto expectedPack,
            int levelNumber,
            out string content,
            out LevelSummary summary)
        {
            content = null;
            summary = default;

            if (!TryFindAvailablePack(
                    expectedPack,
                    out RemoteLevelPackManifestDto manifest,
                    out string versionDirectory))
            {
                return false;
            }

            RemoteLevelEntryDto level = FindLevel(manifest, levelNumber);

            if (level == null ||
                !_diskCache.TryReadBytes(
                    $"{versionDirectory}/{level.ContentPath}",
                    out byte[] levelContent) ||
                !RemoteLevelFileHash.Matches(levelContent, level.Sha256))
            {
                return false;
            }

            try
            {
                content = FileEncoding.GetString(levelContent);
                Enum.TryParse(
                    level.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty);
                summary = new LevelSummary(levelNumber, difficulty);
                return true;
            }
            catch (DecoderFallbackException)
            {
                content = null;
                summary = default;
                return false;
            }
        }

        public bool TryGetLevelSummaries(
            RemoteLevelPackDto expectedPack,
            out IReadOnlyList<LevelSummary> summaries)
        {
            if (!TryFindAvailablePack(
                    expectedPack,
                    out RemoteLevelPackManifestDto manifest,
                    out _))
            {
                summaries = null;
                return false;
            }

            List<LevelSummary> levels = new(manifest.Levels.Count);

            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                RemoteLevelEntryDto level = manifest.Levels[i];
                Enum.TryParse(
                    level.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty);
                levels.Add(new LevelSummary(level.Id.Value, difficulty));
            }

            summaries = levels;
            return true;
        }

        internal bool TryParseManifest(
            byte[] content,
            RemoteLevelPackDto expectedPack,
            out RemoteLevelPackManifestDto manifest)
        {
            try
            {
                string json = FileEncoding.GetString(content);
                return _manifestParser.TryParse(json, out manifest) &&
                       _manifestValidator.IsValid(
                           manifest,
                           expectedPack);
            }
            catch (DecoderFallbackException)
            {
                manifest = null;
                return false;
            }
        }

        public async Task<bool> WriteAtomicallyAsync(
            RemoteLevelPackDto expectedPack,
            byte[] manifestContent,
            IReadOnlyDictionary<string, byte[]> levelContents)
        {
            if (!TryParseManifest(
                    manifestContent,
                    expectedPack,
                    out RemoteLevelPackManifestDto manifest) ||
                !HasExpectedContents(manifest, levelContents))
            {
                return false;
            }

            string versionDirectory = GetVersionDirectory(expectedPack);

            return await _diskCache.WriteDirectoryAtomicallyAsync(
                versionDirectory,
                stagedDirectory => WriteStagedPackAsync(
                    stagedDirectory,
                    manifestContent,
                    manifest,
                    levelContents),
                stagedDirectory => IsStagedPackValid(
                    stagedDirectory,
                    expectedPack));
        }

        internal void DeleteOtherVersions(RemoteLevelPackDto activePack)
        {
            _diskCache.DeleteSubdirectoriesExcept(
                GetPackDirectory(activePack),
                GetVersionDirectoryName(activePack));
        }

        private bool TryFindAvailablePack(
            RemoteLevelPackDto expectedPack,
            out RemoteLevelPackManifestDto manifest,
            out string versionDirectory)
        {
            if (TryLoadPackVersion(
                    expectedPack,
                    expectedPack.Version.Value,
                    out manifest,
                    out versionDirectory))
            {
                return true;
            }

            IReadOnlyList<string> directoryNames =
                _diskCache.GetSubdirectoryNames(
                    GetPackDirectory(expectedPack));
            List<int> versions = new();

            for (int i = 0; i < directoryNames.Count; i++)
            {
                if (TryParseVersionDirectoryName(
                        directoryNames[i],
                        out int version) &&
                    version != expectedPack.Version.Value)
                {
                    versions.Add(version);
                }
            }

            versions.Sort((left, right) => right.CompareTo(left));

            for (int i = 0; i < versions.Count; i++)
            {
                if (TryLoadPackVersion(
                        expectedPack,
                        versions[i],
                        out manifest,
                        out versionDirectory))
                {
                    return true;
                }
            }

            manifest = null;
            versionDirectory = null;
            return false;
        }

        private bool TryLoadPackVersion(
            RemoteLevelPackDto expectedPack,
            int version,
            out RemoteLevelPackManifestDto manifest,
            out string versionDirectory)
        {
            RemoteLevelPackDto versionedPack =
                CreateVersionedPack(expectedPack, version);
            versionDirectory = GetVersionDirectory(versionedPack);
            string manifestPath =
                $"{versionDirectory}/{LocalManifestFileName}";

            if (!IsAvailable(versionedPack) ||
                !_diskCache.TryReadBytes(
                    manifestPath,
                    out byte[] manifestContent) ||
                !TryParseManifest(
                    manifestContent,
                    versionedPack,
                    out manifest))
            {
                manifest = null;
                versionDirectory = null;
                return false;
            }

            return true;
        }

        private static RemoteLevelEntryDto FindLevel(
            RemoteLevelPackManifestDto manifest,
            int levelNumber)
        {
            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                if (manifest.Levels[i].Id == levelNumber)
                {
                    return manifest.Levels[i];
                }
            }

            return null;
        }

        private static RemoteLevelPackDto CreateVersionedPack(
            RemoteLevelPackDto pack,
            int version)
        {
            return new RemoteLevelPackDto
            {
                Id = pack.Id,
                Version = version,
                FirstLevel = pack.FirstLevel,
                LastLevel = pack.LastLevel,
                ManifestPath = pack.ManifestPath
            };
        }

        private static bool TryParseVersionDirectoryName(
            string directoryName,
            out int version)
        {
            const string prefix = "version_";

            if (!directoryName.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                version = 0;
                return false;
            }

            return int.TryParse(
                       directoryName.Substring(prefix.Length),
                       out version) &&
                   version > 0;
        }

        private bool IsStagedPackValid(
            string stagedDirectory,
            RemoteLevelPackDto expectedPack)
        {
            string manifestPath = Path.Combine(stagedDirectory, LocalManifestFileName);

            if (!File.Exists(manifestPath) ||
                !TryParseManifest(
                    File.ReadAllBytes(manifestPath),
                    expectedPack,
                    out RemoteLevelPackManifestDto manifest))
            {
                return false;
            }

            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                if (!IsStagedLevelValid(
                        stagedDirectory,
                        manifest.Levels[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsStagedLevelValid(
            string stagedDirectory,
            RemoteLevelEntryDto level)
        {
            string contentPath = Path.Combine(stagedDirectory, ToSystemPath(level.ContentPath));

            if (!File.Exists(contentPath))
            {
                return false;
            }

            byte[] content = File.ReadAllBytes(contentPath);

            if (!RemoteLevelFileHash.Matches(content, level.Sha256))
            {
                return false;
            }

            try
            {
                if (!_levelParser.TryParse(
                        FileEncoding.GetString(content),
                        out LevelContentDto levelContent,
                        out _))
                {
                    return false;
                }

                Enum.TryParse(
                    level.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty);
                LevelValidationResult validationResult = new();
                _levelValidator.Validate(
                    levelContent,
                    new LevelSummary(level.Id.Value, difficulty),
                    validationResult);
                return validationResult.IsValid;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static bool HasExpectedContents(
            RemoteLevelPackManifestDto manifest,
            IReadOnlyDictionary<string, byte[]> levelContents)
        {
            if (levelContents == null ||
                levelContents.Count != manifest.Levels.Count)
            {
                return false;
            }

            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                RemoteLevelEntryDto level = manifest.Levels[i];

                if (!levelContents.TryGetValue(
                        level.ContentPath,
                        out byte[] content) ||
                    !RemoteLevelFileHash.Matches(
                        content,
                        level.Sha256))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task WriteStagedPackAsync(
            string stagedDirectory,
            byte[] manifestContent,
            RemoteLevelPackManifestDto manifest,
            IReadOnlyDictionary<string, byte[]> levelContents)
        {
            await WriteFileAsync(
                Path.Combine(
                    stagedDirectory,
                    LocalManifestFileName),
                manifestContent);

            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                RemoteLevelEntryDto level = manifest.Levels[i];
                await WriteFileAsync(
                    Path.Combine(
                        stagedDirectory,
                        ToSystemPath(level.ContentPath)),
                    levelContents[level.ContentPath]);
            }
        }

        private static async Task WriteFileAsync(string path, byte[] content)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using FileStream stream = new(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous);
            await stream.WriteAsync(content, 0, content.Length);
            stream.Flush(flushToDisk: true);
        }

        private static string GetPackDirectory(RemoteLevelPackDto pack)
        {
            return $"packs/pack_{pack.Id.Value:D4}";
        }

        private static string GetVersionDirectory(RemoteLevelPackDto pack)
        {
            return $"{GetPackDirectory(pack)}/" +
                   GetVersionDirectoryName(pack);
        }

        private static string GetVersionDirectoryName(RemoteLevelPackDto pack)
        {
            return $"version_{pack.Version.Value:D4}";
        }

        private static string ToSystemPath(string relativePath)
        {
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
