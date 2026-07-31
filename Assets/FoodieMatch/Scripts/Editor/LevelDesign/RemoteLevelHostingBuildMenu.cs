using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal static class RemoteLevelHostingBuildMenu
    {
        [MenuItem("Foodie Match/Level Design/Build Remote Level Packs")]
        private static async void BuildRemoteLevelPacks()
        {
            try
            {
                string projectRoot =
                    Directory.GetParent(Application.dataPath).FullName;
                string outputDirectory =
                    await new RemoteLevelHostingBuilder().BuildAsync(
                        projectRoot);
                Debug.Log(
                    $"Remote level packs were built and validated at " +
                    $"'{outputDirectory}'.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
