using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.Heart;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.UI.Profile;
using NUnit.Framework;
using UnityEngine;

namespace FoodieMatch.Editor.Player
{
    public sealed class PlayerProfileCustomizationTests
    {
        [Test]
        public void PlayerProfile_DefaultCustomizationValues_AreAssignedWhenNullOrEmpty()
        {
            PlayerProfile profile = new(
                currentLevelNumber: 1,
                coinBalance: 100,
                boosterCounts: new Dictionary<BoosterType, int>(),
                seenBoosterGuides: new List<BoosterType>(),
                heartState: new HeartState(5, null),
                playerName: null,
                avatarId: null,
                frameId: null);

            Assert.That(profile.PlayerName, Is.EqualTo("Kandy"));
            Assert.That(profile.AvatarId, Is.EqualTo("avatar_01"));
            Assert.That(profile.FrameId, Is.EqualTo("frame_01"));
        }

        [Test]
        public void PlayerProfile_CustomValues_AreAssignedCorrectly()
        {
            PlayerProfile profile = new(
                currentLevelNumber: 2,
                coinBalance: 200,
                boosterCounts: new Dictionary<BoosterType, int>(),
                seenBoosterGuides: new List<BoosterType>(),
                heartState: new HeartState(5, null),
                playerName: "GourmetChef",
                avatarId: "avatar_03",
                frameId: "frame_02");

            Assert.That(profile.PlayerName, Is.EqualTo("GourmetChef"));
            Assert.That(profile.AvatarId, Is.EqualTo("avatar_03"));
            Assert.That(profile.FrameId, Is.EqualTo("frame_02"));
        }

        [Test]
        public void PlayerProfile_WithCustomization_CreatesUpdatedInstance()
        {
            PlayerProfile original = new(
                currentLevelNumber: 1,
                coinBalance: 50,
                boosterCounts: new Dictionary<BoosterType, int>(),
                seenBoosterGuides: new List<BoosterType>(),
                heartState: new HeartState(5, null));

            PlayerProfile updated = original.WithCustomization(
                "NewName",
                "avatar_04",
                "frame_03");

            Assert.That(updated.PlayerName, Is.EqualTo("NewName"));
            Assert.That(updated.AvatarId, Is.EqualTo("avatar_04"));
            Assert.That(updated.FrameId, Is.EqualTo("frame_03"));
            Assert.That(updated.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(updated.CoinBalance, Is.EqualTo(50));
            Assert.That(original.PlayerName, Is.EqualTo("Kandy"));
        }

        [Test]
        public void ProfileCustomizationCatalog_FallbacksAndDefaults()
        {
            ProfileCustomizationCatalogSO catalog = ScriptableObject.CreateInstance<ProfileCustomizationCatalogSO>();

            Assert.That(catalog.DefaultAvatarId, Is.EqualTo("avatar_01"));
            Assert.That(catalog.DefaultFrameId, Is.EqualTo("frame_01"));
            Assert.That(catalog.TryGetAvatar("unknown", out ProfileCustomizationEntry avatarEntry), Is.False);
            Assert.That(catalog.TryGetFrame("unknown", out ProfileCustomizationEntry frameEntry), Is.False);
        }
    }
}
