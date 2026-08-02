using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Level.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class RemoteLevelHostingBuilder
    {
        private const int SchemaVersion = 1;
        private const int LevelsPerPack = 4;
        private const string SettingsRelativePath =
            "FirebaseHosting/level_build_settings.json";
        private const string CatalogRelativePath =
            "Assets/FoodieMatch/Resources/Data/Levels/level_catalog.json";
        private const string ContentDirectoryRelativePath =
            "Assets/FoodieMatch/Resources/Data/Levels/Content";
        private const string OutputRelativePath =
            "FirebaseHosting/public/levels";

        private static readonly Encoding FileEncoding =
            new UTF8Encoding(false, true);
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Error
        };

        public async Task<RemoteLevelHostingBuildResult> BuildAsync(
            string projectRoot)
        {
            string settingsPath = GetProjectPath(
                projectRoot,
                SettingsRelativePath);
            string catalogPath = GetProjectPath(
                projectRoot,
                CatalogRelativePath);
            string contentDirectory = GetProjectPath(
                projectRoot,
                ContentDirectoryRelativePath);
            string outputDirectory = GetProjectPath(
                projectRoot,
                OutputRelativePath);
            RemoteLevelHostingBuildSettings settings =
                LoadBuildSettings(settingsPath);
            LevelCatalogDto catalog = LoadCatalog(catalogPath);
            IReadOnlyList<LevelBuildContent> levels =
                LoadAndValidateLevels(catalog, contentDirectory);
            ValidateBuildSettings(settings);
            RemoteLevelManifestDto existingManifest =
                LoadExistingManifest(outputDirectory);

            string outputParent = Path.GetDirectoryName(outputDirectory);
            string stagedDirectory = Path.Combine(
                outputParent,
                $".levels_{Guid.NewGuid():N}.staging");

            Directory.CreateDirectory(stagedDirectory);

            try
            {
                CopyExistingArchives(outputDirectory, stagedDirectory);
                List<RemoteLevelPackVersionChange> changedPacks = new();
                RemoteLevelManifestDto manifest = BuildHostedFiles(
                    stagedDirectory,
                    settings,
                    levels,
                    outputDirectory,
                    existingManifest,
                    changedPacks);
                await ValidateHostedFilesAsync(
                    stagedDirectory,
                    manifest);
                ActivateOutput(stagedDirectory, outputDirectory);
                SaveBuildSettings(settingsPath, settings, manifest);
                int previousManifestVersion =
                    existingManifest?.ManifestVersion ?? 0;
                return new RemoteLevelHostingBuildResult(
                    outputDirectory,
                    previousManifestVersion,
                    manifest.ManifestVersion.Value,
                    changedPacks);
            }
            finally
            {
                if (Directory.Exists(stagedDirectory))
                {
                    Directory.Delete(stagedDirectory, recursive: true);
                }
            }
        }

        private static RemoteLevelHostingBuildSettings LoadBuildSettings(
            string settingsPath)
        {
            if (!File.Exists(settingsPath))
            {
                throw new InvalidOperationException(
                    $"Remote level build settings were not found at '{settingsPath}'.");
            }

            try
            {
                return JsonConvert.DeserializeObject<RemoteLevelHostingBuildSettings>(
                    File.ReadAllText(settingsPath, FileEncoding),
                    JsonSettings);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Remote level build settings are invalid: {exception.Message}",
                    exception);
            }
        }

        private static LevelCatalogDto LoadCatalog(string catalogPath)
        {
            if (!File.Exists(catalogPath))
            {
                throw new InvalidOperationException(
                    $"Level catalog was not found at '{catalogPath}'.");
            }

            LevelCatalogJsonParser parser = new();

            if (!parser.TryParse(
                    File.ReadAllText(catalogPath, FileEncoding),
                    out LevelCatalogDto catalog,
                    out string parseError))
            {
                throw new InvalidOperationException(parseError);
            }

            LevelValidationResult validationResult =
                new LevelCatalogValidator().Validate(catalog);
            ThrowIfInvalid(validationResult);
            return catalog;
        }

        private static IReadOnlyList<LevelBuildContent> LoadAndValidateLevels(
            LevelCatalogDto catalog,
            string contentDirectory)
        {
            Dictionary<int, LevelCatalogEntryDto> entries = new();

            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                entries.Add(catalog.Levels[i].Id.Value, catalog.Levels[i]);
            }

            LevelContentJsonParser parser = new();
            LevelContentValidator validator = CreateContentValidator();
            List<LevelBuildContent> levels = new(catalog.LevelOrder.Count);

            for (int i = 0; i < catalog.LevelOrder.Count; i++)
            {
                int levelNumber = catalog.LevelOrder[i];
                LevelCatalogEntryDto entry = entries[levelNumber];
                string contentPath = Path.Combine(
                    contentDirectory,
                    $"{entry.ContentFile}.json");

                if (!File.Exists(contentPath))
                {
                    throw new InvalidOperationException(
                        $"Level {levelNumber} content was not found at '{contentPath}'.");
                }

                byte[] content = File.ReadAllBytes(contentPath);
                string json;

                try
                {
                    json = FileEncoding.GetString(content);
                }
                catch (DecoderFallbackException exception)
                {
                    throw new InvalidOperationException(
                        $"Level {levelNumber} content is not valid UTF-8.",
                        exception);
                }

                if (!parser.TryParse(
                        json,
                        out LevelContentDto levelContent,
                        out string parseError))
                {
                    throw new InvalidOperationException(parseError);
                }

                Enum.TryParse(
                    entry.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty);
                LevelValidationResult validationResult = new();
                validator.Validate(
                    levelContent,
                    new LevelSummary(levelNumber, difficulty),
                    validationResult);
                ThrowIfInvalid(validationResult);
                levels.Add(
                    new LevelBuildContent(
                        levelNumber,
                        difficulty,
                        content));
            }

            ValidateContiguousLevelOrder(levels);
            return levels;
        }

        private static void ValidateBuildSettings(
            RemoteLevelHostingBuildSettings settings)
        {
            if (settings == null ||
                settings.ManifestVersion <= 0 ||
                settings.PackVersions == null)
            {
                throw new InvalidOperationException(
                    "Remote level build settings require a positive manifestVersion " +
                    "and packVersions.");
            }

            for (int i = 0; i < settings.PackVersions.Count; i++)
            {
                if (settings.PackVersions[i] <= 0)
                {
                    throw new InvalidOperationException(
                        $"packVersions[{i}] must be greater than zero.");
                }
            }
        }

        private static RemoteLevelManifestDto BuildHostedFiles(
            string outputDirectory,
            RemoteLevelHostingBuildSettings settings,
            IReadOnlyList<LevelBuildContent> levels,
            string existingOutputDirectory,
            RemoteLevelManifestDto existingManifest,
            ICollection<RemoteLevelPackVersionChange> changedPacks)
        {
            RemoteLevelManifestDto manifest = new()
            {
                SchemaVersion = SchemaVersion,
                Packs = new List<RemoteLevelPackDto>()
            };
            int packCount =
                (levels.Count + LevelsPerPack - 1) / LevelsPerPack;
            string packDirectory = Path.Combine(
                outputDirectory,
                "packs");
            Directory.CreateDirectory(packDirectory);

            for (int packIndex = 0;
                 packIndex < packCount;
                 packIndex++)
            {
                int firstIndex = packIndex * LevelsPerPack;
                int count = Math.Min(
                    LevelsPerPack,
                    levels.Count - firstIndex);
                int packId = packIndex + 1;
                RemoteLevelPackManifestDto packContent =
                    CreatePackManifest(
                        packId,
                        packVersion: 0,
                        levels,
                        firstIndex,
                        count);
                RemoteLevelPackDto existingPack = FindPack(
                    existingManifest,
                    packId);
                RemoteLevelPackDto pack;

                if (CanReusePack(
                        existingOutputDirectory,
                        existingPack,
                        packContent,
                        levels,
                        firstIndex))
                {
                    pack = CopyPack(existingPack);
                }
                else
                {
                    int previousVersion = existingPack?.Version ?? 0;
                    int packVersion = FindNextPackVersion(
                        outputDirectory,
                        packId,
                        previousVersion,
                        settings,
                        packIndex);
                    packContent.PackVersion = packVersion;
                    pack = CreatePack(
                        packId,
                        packVersion,
                        levels[firstIndex].LevelNumber,
                        levels[firstIndex + count - 1].LevelNumber);
                    string archivePath = Path.Combine(
                        outputDirectory,
                        ToSystemPath(pack.ArchivePath));
                    WritePackArchive(
                        archivePath,
                        packContent,
                        levels,
                        firstIndex,
                        count);
                    pack.ArchiveSha256 = RemoteLevelFileHash.Compute(
                        File.ReadAllBytes(archivePath));
                    changedPacks.Add(
                        new RemoteLevelPackVersionChange(
                            packId,
                            previousVersion,
                            packVersion));
                }

                manifest.Packs.Add(pack);
            }

            bool activePacksChanged = changedPacks.Count > 0 ||
                                      HasRemovedPacks(
                                          existingManifest,
                                          packCount);
            manifest.ManifestVersion = ResolveManifestVersion(
                settings.ManifestVersion,
                existingManifest,
                activePacksChanged);

            WriteJson(
                Path.Combine(outputDirectory, "manifest.json"),
                manifest);
            return manifest;
        }

        private static RemoteLevelManifestDto LoadExistingManifest(
            string outputDirectory)
        {
            string manifestPath = Path.Combine(
                outputDirectory,
                "manifest.json");

            if (!File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                RemoteLevelManifestDto manifest =
                    JsonConvert.DeserializeObject<RemoteLevelManifestDto>(
                        File.ReadAllText(manifestPath, FileEncoding),
                        JsonSettings);

                if (manifest?.SchemaVersion != SchemaVersion ||
                    !manifest.ManifestVersion.HasValue ||
                    manifest.ManifestVersion.Value <= 0 ||
                    manifest.Packs == null)
                {
                    throw new InvalidOperationException(
                        "Existing remote level manifest is invalid.");
                }

                return manifest;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Existing remote level manifest is invalid: {exception.Message}",
                    exception);
            }
        }

        private static void CopyExistingArchives(
            string existingOutputDirectory,
            string stagedDirectory)
        {
            string existingPackDirectory = Path.Combine(
                existingOutputDirectory,
                "packs");

            if (!Directory.Exists(existingPackDirectory))
            {
                return;
            }

            string stagedPackDirectory = Path.Combine(
                stagedDirectory,
                "packs");
            Directory.CreateDirectory(stagedPackDirectory);
            string[] archivePaths = Directory.GetFiles(
                existingPackDirectory,
                "*.zip",
                SearchOption.TopDirectoryOnly);

            for (int i = 0; i < archivePaths.Length; i++)
            {
                File.Copy(
                    archivePaths[i],
                    Path.Combine(
                        stagedPackDirectory,
                        Path.GetFileName(archivePaths[i])));
            }
        }

        private static RemoteLevelPackManifestDto CreatePackManifest(
            int packId,
            int packVersion,
            IReadOnlyList<LevelBuildContent> levels,
            int firstIndex,
            int count)
        {
            RemoteLevelPackManifestDto manifest = new()
            {
                SchemaVersion = SchemaVersion,
                PackId = packId,
                PackVersion = packVersion,
                Levels = new List<RemoteLevelEntryDto>()
            };

            for (int i = 0; i < count; i++)
            {
                LevelBuildContent level = levels[firstIndex + i];
                string fileName = $"level_{level.LevelNumber:D4}.json";
                manifest.Levels.Add(
                    new RemoteLevelEntryDto
                    {
                        Id = level.LevelNumber,
                        Difficulty = ToDifficultyName(level.Difficulty),
                        ContentPath = $"levels/{fileName}",
                        Sha256 = RemoteLevelFileHash.Compute(level.Content)
                    });
            }

            return manifest;
        }

        private static RemoteLevelPackDto FindPack(
            RemoteLevelManifestDto manifest,
            int packId)
        {
            if (manifest == null)
            {
                return null;
            }

            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                if (manifest.Packs[i].Id == packId)
                {
                    return manifest.Packs[i];
                }
            }

            return null;
        }

        private static bool CanReusePack(
            string existingOutputDirectory,
            RemoteLevelPackDto existingPack,
            RemoteLevelPackManifestDto expectedContent,
            IReadOnlyList<LevelBuildContent> levels,
            int firstIndex)
        {
            if (existingPack?.Version == null ||
                string.IsNullOrEmpty(existingPack.ArchivePath) ||
                string.IsNullOrEmpty(existingPack.ArchiveSha256))
            {
                return false;
            }

            string archivePath = Path.Combine(
                existingOutputDirectory,
                ToSystemPath(existingPack.ArchivePath));

            if (!File.Exists(archivePath))
            {
                return false;
            }

            byte[] archiveContent = File.ReadAllBytes(archivePath);

            if (!RemoteLevelFileHash.Matches(
                    archiveContent,
                    existingPack.ArchiveSha256))
            {
                return false;
            }

            RemoteLevelPackArchiveReader archiveReader = new();

            if (!archiveReader.TryRead(
                    archiveContent,
                    out byte[] manifestContent,
                    out IReadOnlyDictionary<string, byte[]> levelContents))
            {
                return false;
            }

            try
            {
                RemoteLevelPackManifestDto existingContent =
                    JsonConvert.DeserializeObject<RemoteLevelPackManifestDto>(
                        FileEncoding.GetString(manifestContent),
                        JsonSettings);
                return HasSamePackContent(
                    existingContent,
                    expectedContent,
                    levelContents,
                    levels,
                    firstIndex);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasSamePackContent(
            RemoteLevelPackManifestDto existingContent,
            RemoteLevelPackManifestDto expectedContent,
            IReadOnlyDictionary<string, byte[]> existingLevelContents,
            IReadOnlyList<LevelBuildContent> levels,
            int firstIndex)
        {
            if (existingContent?.SchemaVersion != expectedContent.SchemaVersion ||
                existingContent.PackId != expectedContent.PackId ||
                existingContent.Levels == null ||
                existingContent.Levels.Count != expectedContent.Levels.Count)
            {
                return false;
            }

            for (int i = 0; i < existingContent.Levels.Count; i++)
            {
                RemoteLevelEntryDto existingLevel = existingContent.Levels[i];
                RemoteLevelEntryDto expectedLevel = expectedContent.Levels[i];

                if (existingLevel?.Id != expectedLevel.Id ||
                    !string.Equals(
                        existingLevel.Difficulty,
                        expectedLevel.Difficulty,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        existingLevel.ContentPath,
                        expectedLevel.ContentPath,
                        StringComparison.Ordinal) ||
                    !existingLevelContents.TryGetValue(
                        existingLevel.ContentPath,
                        out byte[] existingLevelContent) ||
                    !HasSameJsonContent(
                        existingLevelContent,
                        levels[firstIndex + i].Content))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameJsonContent(
            byte[] existingContent,
            byte[] expectedContent)
        {
            try
            {
                JToken existingJson = JToken.Parse(
                    FileEncoding.GetString(existingContent));
                JToken expectedJson = JToken.Parse(
                    FileEncoding.GetString(expectedContent));
                return JToken.DeepEquals(existingJson, expectedJson);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static int FindNextPackVersion(
            string outputDirectory,
            int packId,
            int previousVersion,
            RemoteLevelHostingBuildSettings settings,
            int packIndex)
        {
            int configuredVersion = packIndex < settings.PackVersions.Count
                ? settings.PackVersions[packIndex]
                : 1;
            int version = previousVersion > 0
                ? Math.Max(previousVersion + 1, configuredVersion)
                : configuredVersion;

            while (File.Exists(
                       Path.Combine(
                           outputDirectory,
                           "packs",
                           $"pack_{packId:D4}_v{version:D4}.zip")))
            {
                version++;
            }

            return version;
        }

        private static RemoteLevelPackDto CreatePack(
            int packId,
            int packVersion,
            int firstLevel,
            int lastLevel)
        {
            return new RemoteLevelPackDto
            {
                Id = packId,
                Version = packVersion,
                FirstLevel = firstLevel,
                LastLevel = lastLevel,
                ArchivePath =
                    $"packs/pack_{packId:D4}_v{packVersion:D4}.zip"
            };
        }

        private static RemoteLevelPackDto CopyPack(
            RemoteLevelPackDto pack)
        {
            return new RemoteLevelPackDto
            {
                Id = pack.Id,
                Version = pack.Version,
                FirstLevel = pack.FirstLevel,
                LastLevel = pack.LastLevel,
                ArchivePath = pack.ArchivePath,
                ArchiveSha256 = pack.ArchiveSha256
            };
        }

        private static bool HasRemovedPacks(
            RemoteLevelManifestDto existingManifest,
            int packCount)
        {
            return existingManifest != null &&
                   existingManifest.Packs.Count != packCount;
        }

        private static int ResolveManifestVersion(
            int configuredVersion,
            RemoteLevelManifestDto existingManifest,
            bool activePacksChanged)
        {
            if (existingManifest == null)
            {
                return Math.Max(1, configuredVersion);
            }

            int previousVersion = existingManifest.ManifestVersion.Value;
            return activePacksChanged
                ? Math.Max(previousVersion + 1, configuredVersion)
                : Math.Max(previousVersion, configuredVersion);
        }

        private static void SaveBuildSettings(
            string settingsPath,
            RemoteLevelHostingBuildSettings settings,
            RemoteLevelManifestDto manifest)
        {
            settings.ManifestVersion = manifest.ManifestVersion.Value;
            settings.PackVersions.Clear();

            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                settings.PackVersions.Add(
                    manifest.Packs[i].Version.Value);
            }

            WriteJson(settingsPath, settings);
        }

        private static async Task ValidateHostedFilesAsync(
            string outputDirectory,
            RemoteLevelManifestDto manifest)
        {
            string validationDirectory = Path.Combine(
                Path.GetTempPath(),
                $"FoodieMatchRemoteLevelValidation_{Guid.NewGuid():N}");
            LevelDiskCache diskCache = new(validationDirectory);

            try
            {
                RemoteLevelManifestCache manifestCache = new(diskCache);
                string manifestJson = File.ReadAllText(
                    Path.Combine(outputDirectory, "manifest.json"),
                    FileEncoding);

                if (!await manifestCache.WriteAtomicallyAsync(
                        manifestJson,
                        manifest.ManifestVersion))
                {
                    throw new InvalidOperationException(
                        "Generated root level manifest failed runtime validation.");
                }

                RemoteLevelPackCache packCache = new(
                    diskCache,
                    new LevelContentJsonParser(),
                    CreateContentValidator());
                RemoteLevelPackArchiveReader archiveReader = new();

                for (int i = 0; i < manifest.Packs.Count; i++)
                {
                    RemoteLevelPackDto pack = manifest.Packs[i];
                    string archivePath = Path.Combine(
                        outputDirectory,
                        ToSystemPath(pack.ArchivePath));
                    byte[] archiveContent = File.ReadAllBytes(
                        archivePath);

                    if (!RemoteLevelFileHash.Matches(
                            archiveContent,
                            pack.ArchiveSha256) ||
                        !archiveReader.TryRead(
                            archiveContent,
                            out byte[] packManifestContent,
                            out IReadOnlyDictionary<string, byte[]>
                                levelContents))
                    {
                        throw new InvalidOperationException(
                            $"Generated level pack {pack.Id.Value} " +
                            "archive failed runtime validation.");
                    }

                    if (!await packCache.WriteAtomicallyAsync(
                            pack,
                            packManifestContent,
                            levelContents))
                    {
                        throw new InvalidOperationException(
                            $"Generated level pack {pack.Id.Value} " +
                            "failed runtime validation.");
                    }
                }
            }
            finally
            {
                if (Directory.Exists(validationDirectory))
                {
                    Directory.Delete(
                        validationDirectory,
                        recursive: true);
                }
            }
        }

        private static void WritePackArchive(
            string archivePath,
            RemoteLevelPackManifestDto manifest,
            IReadOnlyList<LevelBuildContent> levels,
            int firstIndex,
            int count)
        {
            using FileStream archiveStream = new(
                archivePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            using ZipArchive archive = new(
                archiveStream,
                ZipArchiveMode.Create);
            WriteArchiveEntry(
                archive,
                "pack_manifest.json",
                SerializeJson(manifest));

            for (int i = 0; i < count; i++)
            {
                LevelBuildContent level = levels[firstIndex + i];
                WriteArchiveEntry(
                    archive,
                    $"levels/level_{level.LevelNumber:D4}.json",
                    level.Content);
            }
        }

        private static void WriteArchiveEntry(
            ZipArchive archive,
            string path,
            byte[] content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(
                path,
                CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(
                1980,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);
            using Stream entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }

        private static LevelContentValidator CreateContentValidator()
        {
            LevelValidator levelValidator = new(
                new PackageSelectionSettingsValidator(),
                new LevelRandomSettingsValidator(),
                new GrillLayoutValidator(),
                new GrillMovementGroupValidator());
            return new LevelContentValidator(levelValidator);
        }

        private static void ValidateContiguousLevelOrder(
            IReadOnlyList<LevelBuildContent> levels)
        {
            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i].LevelNumber !=
                    levels[i - 1].LevelNumber + 1)
                {
                    throw new InvalidOperationException(
                        "Remote level order must contain contiguous level ids.");
                }
            }
        }

        private static void ThrowIfInvalid(
            LevelValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors));
            }
        }

        private static void WriteJson(string path, object value)
        {
            File.WriteAllBytes(path, SerializeJson(value));
        }

        private static byte[] SerializeJson(object value)
        {
            string json = JsonConvert.SerializeObject(
                value,
                Formatting.Indented,
                JsonSettings);
            return FileEncoding.GetBytes(json);
        }

        private static void ActivateOutput(
            string stagedDirectory,
            string outputDirectory)
        {
            string outputParent = Path.GetDirectoryName(outputDirectory);
            Directory.CreateDirectory(outputParent);

            if (!Directory.Exists(outputDirectory))
            {
                Directory.Move(stagedDirectory, outputDirectory);
                return;
            }

            string backupDirectory =
                $"{outputDirectory}.{Guid.NewGuid():N}.backup";
            Directory.Move(outputDirectory, backupDirectory);

            try
            {
                Directory.Move(stagedDirectory, outputDirectory);
                Directory.Delete(backupDirectory, recursive: true);
            }
            catch
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.Move(
                        backupDirectory,
                        outputDirectory);
                }

                throw;
            }
        }

        private static string GetProjectPath(
            string projectRoot,
            string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    projectRoot,
                    ToSystemPath(relativePath)));
        }

        private static string ToSystemPath(string path)
        {
            return path.Replace(
                '/',
                Path.DirectorySeparatorChar);
        }

        private static string ToDifficultyName(
            LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Normal => "normal",
                LevelDifficulty.Hard => "hard",
                LevelDifficulty.SuperHard => "superHard",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(difficulty),
                    difficulty,
                    null)
            };
        }

        private readonly struct LevelBuildContent
        {
            public LevelBuildContent(
                int levelNumber,
                LevelDifficulty difficulty,
                byte[] content)
            {
                LevelNumber = levelNumber;
                Difficulty = difficulty;
                Content = content;
            }

            public int LevelNumber { get; }
            public LevelDifficulty Difficulty { get; }
            public byte[] Content { get; }
        }
    }

    internal sealed class RemoteLevelHostingBuildResult
    {
        public RemoteLevelHostingBuildResult(
            string outputDirectory,
            int previousManifestVersion,
            int manifestVersion,
            IReadOnlyList<RemoteLevelPackVersionChange> changedPacks)
        {
            OutputDirectory = outputDirectory;
            PreviousManifestVersion = previousManifestVersion;
            ManifestVersion = manifestVersion;
            ChangedPacks = changedPacks;
        }

        public string OutputDirectory { get; }

        public int PreviousManifestVersion { get; }

        public int ManifestVersion { get; }

        public IReadOnlyList<RemoteLevelPackVersionChange> ChangedPacks { get; }

        public bool ManifestVersionChanged =>
            ManifestVersion != PreviousManifestVersion;
    }

    internal readonly struct RemoteLevelPackVersionChange
    {
        public RemoteLevelPackVersionChange(
            int packId,
            int previousVersion,
            int version)
        {
            PackId = packId;
            PreviousVersion = previousVersion;
            Version = version;
        }

        public int PackId { get; }

        public int PreviousVersion { get; }

        public int Version { get; }
    }
}
