using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSeedValidationReportWriter
    {
        private const string ReportPath = "Temp/LevelSeedValidationReport.json";

        public string FullReportPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));

        public void Save(LevelSeedValidationReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FullReportPath));
            File.WriteAllText(
                FullReportPath,
                JsonConvert.SerializeObject(report, Formatting.Indented));
        }
    }
}
