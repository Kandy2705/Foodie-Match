using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Level.Remote;
using NUnit.Framework;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class RemoteLevelPackCacheTests
    {
        private static readonly string LevelContentPath = Path.Combine(
            Application.dataPath,
            "FoodieMatch/Resources/Data/Levels/Content/level_0001.json");

        private string _testDirectory;
        private RemoteLevelPackCache _cache;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"FoodieMatchPackCacheTests_{Guid.NewGuid():N}");
            _cache = new RemoteLevelPackCache(
                new LevelDiskCache(_testDirectory),
                new LevelContentJsonParser(),
                CreateLevelContentValidator());
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
        public async Task WriteAtomicallyAsync_ValidPack_ActivatesPack()
        {
            byte[] levelContent = File.ReadAllBytes(LevelContentPath);
            RemoteLevelPackDto pack = CreatePack(version: 1);
            byte[] manifestContent = CreateManifest(
                packVersion: 1,
                CalculateSha256(levelContent));
            Dictionary<string, byte[]> levelContents = new()
            {
                ["level_0001.json"] = levelContent
            };

            bool written = await _cache.WriteAtomicallyAsync(
                pack,
                manifestContent,
                levelContents);

            Assert.That(written, Is.True);
            Assert.That(_cache.IsAvailable(pack), Is.True);
        }

        [Test]
        public async Task WriteAtomicallyAsync_InvalidHash_RejectsPack()
        {
            byte[] levelContent = File.ReadAllBytes(LevelContentPath);
            RemoteLevelPackDto pack = CreatePack(version: 1);
            byte[] manifestContent = CreateManifest(
                packVersion: 1,
                new string('0', 64));
            Dictionary<string, byte[]> levelContents = new()
            {
                ["level_0001.json"] = levelContent
            };

            bool written = await _cache.WriteAtomicallyAsync(
                pack,
                manifestContent,
                levelContents);

            Assert.That(written, Is.False);
            Assert.That(_cache.IsAvailable(pack), Is.False);
        }

        [Test]
        public async Task TryReadLevel_NewVersionMissing_ReadsPreviousVersion()
        {
            byte[] levelContent = File.ReadAllBytes(LevelContentPath);
            string sha256 = CalculateSha256(levelContent);
            Dictionary<string, byte[]> levelContents = new()
            {
                ["level_0001.json"] = levelContent
            };

            await _cache.WriteAtomicallyAsync(
                CreatePack(version: 1),
                CreateManifest(packVersion: 1, sha256),
                levelContents);
            bool loaded = _cache.TryReadLevel(
                CreatePack(version: 2),
                levelNumber: 1,
                out string content,
                out _);

            Assert.That(loaded, Is.True);
            Assert.That(content, Is.EqualTo(Encoding.UTF8.GetString(levelContent)));
        }

        private static LevelContentValidator CreateLevelContentValidator()
        {
            LevelValidator levelValidator = new(
                new PackageSelectionSettingsValidator(),
                new LevelRandomSettingsValidator(),
                new GrillLayoutValidator(),
                new GrillMovementGroupValidator());
            return new LevelContentValidator(levelValidator);
        }

        private static RemoteLevelPackDto CreatePack(int version)
        {
            return new RemoteLevelPackDto
            {
                Id = 1,
                Version = version,
                FirstLevel = 1,
                LastLevel = 1,
                ManifestPath =
                    "packs/pack_0001/pack_manifest.json"
            };
        }

        private static byte[] CreateManifest(
            int packVersion,
            string sha256)
        {
            string json =
                "{" +
                "\"schemaVersion\":1," +
                "\"packId\":1," +
                $"\"packVersion\":{packVersion}," +
                "\"levels\":[" +
                "{" +
                "\"id\":1," +
                "\"difficulty\":\"normal\"," +
                "\"contentPath\":\"level_0001.json\"," +
                $"\"sha256\":\"{sha256}\"" +
                "}" +
                "]" +
                "}";
            return Encoding.UTF8.GetBytes(json);
        }

        private static string CalculateSha256(byte[] content)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(content);
            StringBuilder builder = new(hash.Length * 2);

            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
