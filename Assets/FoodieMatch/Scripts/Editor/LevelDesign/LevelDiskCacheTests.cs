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
    }
}
