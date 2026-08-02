using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class RemoteLevelHostingBuilderTests
    {
        private static readonly string LevelDataDirectory = Path.Combine(
            Application.dataPath,
            "FoodieMatch/Resources/Data/Levels");

        private string _projectRoot;

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(
                Path.GetTempPath(),
                $"FoodieMatchHostingBuilderTests_{Guid.NewGuid():N}");
            CopyLevelData();
            WriteBuildSettings();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_projectRoot))
            {
                Directory.Delete(_projectRoot, recursive: true);
            }
        }

        [Test]
        public async Task BuildAsync_OnlyChangedPack_CreatesNewVersion()
        {
            RemoteLevelHostingBuilder builder = new();
            RemoteLevelHostingBuildResult initialResult =
                await builder.BuildAsync(_projectRoot);
            string packTwoPath = GetPackPath(packId: 2, version: 1);
            byte[] initialPackTwoContent = File.ReadAllBytes(packTwoPath);
            string levelOnePath = GetLevelContentPath(levelNumber: 1);

            File.AppendAllText(levelOnePath, Environment.NewLine);
            RemoteLevelHostingBuildResult formattingResult =
                await builder.BuildAsync(_projectRoot);

            string levelContent = File.ReadAllText(levelOnePath);
            File.WriteAllText(
                levelOnePath,
                levelContent.Replace("\"y\": 0", "\"y\": 0.25"),
                new UTF8Encoding(false));
            RemoteLevelHostingBuildResult contentResult =
                await builder.BuildAsync(_projectRoot);

            Assert.That(initialResult.ManifestVersion, Is.EqualTo(1));
            Assert.That(formattingResult.ChangedPacks, Is.Empty);
            Assert.That(formattingResult.ManifestVersion, Is.EqualTo(1));
            Assert.That(contentResult.ChangedPacks.Count, Is.EqualTo(1));
            Assert.That(contentResult.ChangedPacks[0].PackId, Is.EqualTo(1));
            Assert.That(
                contentResult.ChangedPacks[0].PreviousVersion,
                Is.EqualTo(1));
            Assert.That(contentResult.ChangedPacks[0].Version, Is.EqualTo(2));
            Assert.That(contentResult.ManifestVersion, Is.EqualTo(2));
            Assert.That(File.Exists(GetPackPath(packId: 1, version: 1)), Is.True);
            Assert.That(File.Exists(GetPackPath(packId: 1, version: 2)), Is.True);
            CollectionAssert.AreEqual(
                initialPackTwoContent,
                File.ReadAllBytes(packTwoPath));
        }

        private void CopyLevelData()
        {
            string targetDirectory = Path.Combine(
                _projectRoot,
                "Assets/FoodieMatch/Resources/Data/Levels");
            Directory.CreateDirectory(targetDirectory);
            File.Copy(
                Path.Combine(LevelDataDirectory, "level_catalog.json"),
                Path.Combine(targetDirectory, "level_catalog.json"));
            string contentDirectory = Path.Combine(
                targetDirectory,
                "Content");
            Directory.CreateDirectory(contentDirectory);
            string[] levelPaths = Directory.GetFiles(
                Path.Combine(LevelDataDirectory, "Content"),
                "*.json",
                SearchOption.TopDirectoryOnly);

            for (int i = 0; i < levelPaths.Length; i++)
            {
                File.Copy(
                    levelPaths[i],
                    Path.Combine(
                        contentDirectory,
                        Path.GetFileName(levelPaths[i])));
            }
        }

        private void WriteBuildSettings()
        {
            string hostingDirectory = Path.Combine(
                _projectRoot,
                "FirebaseHosting");
            Directory.CreateDirectory(hostingDirectory);
            File.WriteAllText(
                Path.Combine(
                    hostingDirectory,
                    "level_build_settings.json"),
                "{\"manifestVersion\":1,\"packVersions\":[1,1,1,1]}");
        }

        private string GetLevelContentPath(int levelNumber)
        {
            return Path.Combine(
                _projectRoot,
                "Assets/FoodieMatch/Resources/Data/Levels/Content",
                $"level_{levelNumber:D4}.json");
        }

        private string GetPackPath(int packId, int version)
        {
            return Path.Combine(
                _projectRoot,
                "FirebaseHosting/public/levels/packs",
                $"pack_{packId:D4}_v{version:D4}.zip");
        }
    }
}
