using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level;
using NUnit.Framework;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelCatalogRepositoryTests
    {
        [Test]
        public void SetRemoteLevels_OverridesLocalMetadataAndAddsLevels()
        {
            LevelCatalog localCatalog = new(
                new List<LevelSummary>
                {
                    new(1, LevelDifficulty.Normal),
                    new(2, LevelDifficulty.Hard)
                });
            LevelCatalogRepository repository = new(localCatalog);

            repository.SetRemoteLevels(
                new List<LevelSummary>
                {
                    new(2, LevelDifficulty.SuperHard),
                    new(3, LevelDifficulty.Hard)
                });

            Assert.That(
                repository.TryGetLevelSummary(
                    2,
                    out LevelSummary secondLevel),
                Is.True);
            Assert.That(
                secondLevel.Difficulty,
                Is.EqualTo(LevelDifficulty.SuperHard));
            Assert.That(
                repository.TryGetNextLevelSummary(
                    2,
                    out LevelSummary nextLevel),
                Is.True);
            Assert.That(nextLevel.LevelNumber, Is.EqualTo(3));
        }
    }
}
