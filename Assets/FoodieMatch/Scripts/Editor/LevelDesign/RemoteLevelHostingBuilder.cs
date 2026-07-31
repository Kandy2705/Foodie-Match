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

        public async Task<string> BuildAsync(string projectRoot)
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
            ValidateBuildSettings(settings, levels.Count);

            string outputParent = Path.GetDirectoryName(outputDirectory);
            string stagedDirectory = Path.Combine(
                outputParent,
                $".levels_{Guid.NewGuid():N}.staging");

            Directory.CreateDirectory(stagedDirectory);

            try
            {
                RemoteLevelManifestDto manifest = BuildHostedFiles(
                    stagedDirectory,
                    settings,
                    levels);
                await ValidateHostedFilesAsync(
                    stagedDirectory,
                    manifest);
                ActivateOutput(stagedDirectory, outputDirectory);
                return outputDirectory;
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
            RemoteLevelHostingBuildSettings settings,
            int levelCount)
        {
            if (settings == null ||
                settings.ManifestVersion <= 0 ||
                settings.PackVersions == null)
            {
                throw new InvalidOperationException(
                    "Remote level build settings require a positive manifestVersion " +
                    "and packVersions.");
            }

            int packCount =
                (levelCount + LevelsPerPack - 1) / LevelsPerPack;

            if (settings.PackVersions.Count != packCount)
            {
                throw new InvalidOperationException(
                    $"packVersions must contain {packCount} entries.");
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
            IReadOnlyList<LevelBuildContent> levels)
        {
            RemoteLevelManifestDto manifest = new()
            {
                SchemaVersion = SchemaVersion,
                ManifestVersion = settings.ManifestVersion,
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
                int packVersion = settings.PackVersions[packIndex];
                string archiveRelativePath =
                    $"packs/pack_{packId:D4}_v{packVersion:D4}.zip";
                RemoteLevelPackDto pack = new()
                {
                    Id = packId,
                    Version = packVersion,
                    FirstLevel = levels[firstIndex].LevelNumber,
                    LastLevel = levels[firstIndex + count - 1].LevelNumber,
                    ArchivePath = archiveRelativePath
                };
                RemoteLevelPackManifestDto packManifest = new()
                {
                    SchemaVersion = SchemaVersion,
                    PackId = pack.Id,
                    PackVersion = pack.Version,
                    Levels = new List<RemoteLevelEntryDto>()
                };

                for (int i = 0; i < count; i++)
                {
                    LevelBuildContent level = levels[firstIndex + i];
                    string fileName =
                        $"level_{level.LevelNumber:D4}.json";
                    string contentPath = $"levels/{fileName}";
                    packManifest.Levels.Add(
                        new RemoteLevelEntryDto
                        {
                            Id = level.LevelNumber,
                            Difficulty =
                                ToDifficultyName(level.Difficulty),
                            ContentPath = contentPath,
                            Sha256 = RemoteLevelFileHash.Compute(
                                level.Content)
                        });
                }

                string archivePath = Path.Combine(
                    outputDirectory,
                    ToSystemPath(archiveRelativePath));
                WritePackArchive(
                    archivePath,
                    packManifest,
                    levels,
                    firstIndex,
                    count);
                pack.ArchiveSha256 = RemoteLevelFileHash.Compute(
                    File.ReadAllBytes(archivePath));
                manifest.Packs.Add(pack);
            }

            WriteJson(
                Path.Combine(outputDirectory, "manifest.json"),
                manifest);
            return manifest;
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
}
