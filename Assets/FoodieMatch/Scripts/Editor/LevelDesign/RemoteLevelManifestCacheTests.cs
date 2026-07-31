using System;
using System.IO;
using System.Threading.Tasks;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Remote;
using NUnit.Framework;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class RemoteLevelManifestCacheTests
    {
        private string _testDirectory;
        private RemoteLevelManifestCache _cache;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"FoodieMatchManifestCacheTests_{Guid.NewGuid():N}");
            _cache = new RemoteLevelManifestCache(
                new LevelDiskCache(_testDirectory));
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
        public async Task WriteAtomicallyAsync_ValidManifest_CachesManifest()
        {
            string content = CreateManifest(
                manifestVersion: 2,
                secondPackFirstLevel: 5,
                secondPackPath: "packs/pack_0002_v0001.zip");

            bool written = await _cache.WriteAtomicallyAsync(
                content,
                expectedManifestVersion: 2);

            Assert.That(written, Is.True);
            Assert.That(
                _cache.TryLoad(out RemoteLevelManifestDto manifest),
                Is.True);
            Assert.That(manifest.ManifestVersion, Is.EqualTo(2));
        }

        [Test]
        public async Task WriteAtomicallyAsync_UnexpectedVersion_RejectsManifest()
        {
            string content = CreateManifest(
                manifestVersion: 2,
                secondPackFirstLevel: 5,
                secondPackPath: "packs/pack_0002_v0001.zip");

            bool written = await _cache.WriteAtomicallyAsync(
                content,
                expectedManifestVersion: 3);

            Assert.That(written, Is.False);
            Assert.That(_cache.TryLoad(out _), Is.False);
        }

        [Test]
        public async Task WriteAtomicallyAsync_OverlappingPacks_RejectsManifest()
        {
            string content = CreateManifest(
                manifestVersion: 1,
                secondPackFirstLevel: 4,
                secondPackPath: "packs/pack_0002_v0001.zip");

            bool written = await _cache.WriteAtomicallyAsync(
                content,
                expectedManifestVersion: 1);

            Assert.That(written, Is.False);
        }

        [Test]
        public async Task WriteAtomicallyAsync_UnsafePath_RejectsManifest()
        {
            string content = CreateManifest(
                manifestVersion: 1,
                secondPackFirstLevel: 5,
                secondPackPath: "../pack_0002.zip");

            bool written = await _cache.WriteAtomicallyAsync(
                content,
                expectedManifestVersion: 1);

            Assert.That(written, Is.False);
        }

        private static string CreateManifest(
            int manifestVersion,
            int secondPackFirstLevel,
            string secondPackPath)
        {
            return
                "{" +
                "\"schemaVersion\":1," +
                $"\"manifestVersion\":{manifestVersion}," +
                "\"packs\":[" +
                "{" +
                "\"id\":1," +
                "\"version\":1," +
                "\"firstLevel\":1," +
                "\"lastLevel\":4," +
                "\"archivePath\":\"packs/pack_0001_v0001.zip\"," +
                $"\"archiveSha256\":\"{new string('a', 64)}\"" +
                "}," +
                "{" +
                "\"id\":2," +
                "\"version\":1," +
                $"\"firstLevel\":{secondPackFirstLevel}," +
                "\"lastLevel\":8," +
                $"\"archivePath\":\"{secondPackPath}\"," +
                $"\"archiveSha256\":\"{new string('b', 64)}\"" +
                "}" +
                "]" +
                "}";
        }
    }
}
