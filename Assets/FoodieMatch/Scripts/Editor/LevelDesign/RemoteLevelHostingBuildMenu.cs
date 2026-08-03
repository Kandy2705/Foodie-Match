using System;
using System.IO;
using System.Text;
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
                RemoteLevelHostingBuildResult result =
                    await new RemoteLevelHostingBuilder().BuildAsync(
                        projectRoot);
                Debug.Log(CreateBuildMessage(result));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static string CreateBuildMessage(
            RemoteLevelHostingBuildResult result)
        {
            StringBuilder message = new();
            message.AppendLine(
                $"Remote level packs were built and validated at " +
                $"'{result.OutputDirectory}'.");

            if (result.ChangedPacks.Count == 0)
            {
                message.AppendLine("No pack content changes were found.");
            }
            else
            {
                message.AppendLine("Changed packs:");

                for (int i = 0; i < result.ChangedPacks.Count; i++)
                {
                    RemoteLevelPackVersionChange change =
                        result.ChangedPacks[i];
                    message.AppendLine(
                        $"- Pack {change.PackId}: " +
                        $"version {change.PreviousVersion} -> " +
                        $"{change.Version}");
                }
            }

            string versionStatus = result.ManifestVersionChanged
                ? "Update Remote Config after deploying Hosting:"
                : "Remote Config remains unchanged:";
            message.AppendLine(versionStatus);
            message.Append(
                $"levels_manifest_version = {result.ManifestVersion}");
            return message.ToString();
        }
    }
}
