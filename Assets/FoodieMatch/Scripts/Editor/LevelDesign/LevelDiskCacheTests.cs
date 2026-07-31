using System;
using System.IO;
using System.Threading.Tasks;
using FoodieMatch.Infrastructure.Level;
using NUnit.Framework;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelDiskCacheTests
    {
        private string _testDirectory;
        private string _cacheDirectory;
        private LevelDiskCache _cache;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"FoodieMatchLevelCacheTests_{Guid.NewGuid():N}");
            _cacheDirectory = Path.Combine(
                _testDirectory,
                "LevelCache");
            _cache = new LevelDiskCache(_cacheDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [Test]
        public async Task WriteFileAtomicallyAsync_ValidContent_ReplacesActiveFile()
        {
            const string relativePath = "packs/pack_0001/level_0001.json";
            const string originalContent = "{\"version\":1}";
            const string updatedContent = "{\"version\":2}";

            await _cache.WriteFileAtomicallyAsync(
                relativePath,
                originalContent,
                content => content == originalContent);
            bool written = await _cache.WriteFileAtomicallyAsync(
                relativePath,
                updatedContent,
                content => content == updatedContent);

            Assert.That(written, Is.True);
            Assert.That(
                _cache.TryReadFile(relativePath, out string content),
                Is.True);
            Assert.That(content, Is.EqualTo(updatedContent));
        }

        [Test]
        public async Task WriteFileAtomicallyAsync_InvalidContent_PreservesActiveFile()
        {
            const string relativePath = "manifest/manifest.json";
            const string activeContent = "{\"version\":1}";

            await _cache.WriteFileAtomicallyAsync(
                relativePath,
                activeContent,
                content => content == activeContent);
            bool written = await _cache.WriteFileAtomicallyAsync(
                relativePath,
                "{\"version\":2}",
                content => false);

            Assert.That(written, Is.False);
            Assert.That(
                _cache.TryReadFile(relativePath, out string content),
                Is.True);
            Assert.That(content, Is.EqualTo(activeContent));
        }

        [Test]
        public async Task WriteDirectoryAtomicallyAsync_InvalidContent_PreservesActiveDirectory()
        {
            const string relativePath = "packs/pack_0001/version_0001";

            await _cache.WriteDirectoryAtomicallyAsync(
                relativePath,
                directory =>
                {
                    File.WriteAllText(
                        Path.Combine(directory, "level.json"),
                        "original");
                    return Task.CompletedTask;
                },
                directory => File.Exists(
                    Path.Combine(directory, "level.json")));
            bool written = await _cache.WriteDirectoryAtomicallyAsync(
                relativePath,
                directory =>
                {
                    File.WriteAllText(
                        Path.Combine(directory, "level.json"),
                        "updated");
                    return Task.CompletedTask;
                },
                directory => false);

            Assert.That(written, Is.False);
            Assert.That(
                _cache.TryReadFile(
                    $"{relativePath}/level.json",
                    out string content),
                Is.True);
            Assert.That(content, Is.EqualTo("original"));
        }

        [Test]
        public async Task WriteDirectoryAtomicallyAsync_ValidContent_ReplacesActiveDirectory()
        {
            const string relativePath = "packs/pack_0001/version_0001";

            await _cache.WriteDirectoryAtomicallyAsync(
                relativePath,
                directory => WriteDirectoryContent(directory, "original"),
                directory => HasDirectoryContent(directory, "original"));
            bool written = await _cache.WriteDirectoryAtomicallyAsync(
                relativePath,
                directory => WriteDirectoryContent(directory, "updated"),
                directory => HasDirectoryContent(directory, "updated"));

            Assert.That(written, Is.True);
            Assert.That(
                _cache.TryReadFile($"{relativePath}/level.json", out string content),
                Is.True);
            Assert.That(content, Is.EqualTo("updated"));
        }

        [Test]
        public void DeleteSubdirectoriesExcept_RemovesInactiveVersions()
        {
            string packDirectory = Path.Combine(_cacheDirectory, "packs", "pack_0001");
            Directory.CreateDirectory(Path.Combine(packDirectory, "version_0001"));
            Directory.CreateDirectory(Path.Combine(packDirectory, "version_0002"));

            _cache.DeleteSubdirectoriesExcept("packs/pack_0001", "version_0002");

            Assert.That(Directory.Exists(Path.Combine(packDirectory, "version_0001")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(packDirectory, "version_0002")), Is.True);
        }

        [Test]
        public void ClearStaging_RemovesStaleFiles()
        {
            string stagingDirectory = Path.Combine(
                _cacheDirectory,
                "staging");
            Directory.CreateDirectory(stagingDirectory);
            File.WriteAllText(
                Path.Combine(stagingDirectory, "stale.tmp"),
                "incomplete");

            _cache.ClearStaging();

            Assert.That(Directory.Exists(stagingDirectory), Is.False);
        }

        [Test]
        public void TryReadFile_PathOutsideCache_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => _cache.TryReadFile("../outside.json", out _));
        }

        private static Task WriteDirectoryContent(string directory, string content)
        {
            File.WriteAllText(Path.Combine(directory, "level.json"), content);
            return Task.CompletedTask;
        }

        private static bool HasDirectoryContent(string directory, string expectedContent)
        {
            string path = Path.Combine(directory, "level.json");
            return File.Exists(path) && File.ReadAllText(path) == expectedContent;
        }
    }
}
