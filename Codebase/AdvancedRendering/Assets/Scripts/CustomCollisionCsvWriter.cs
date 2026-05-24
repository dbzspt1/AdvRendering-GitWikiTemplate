using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AdvancedRendering.CustomCollisionBenchmark
{
    public static class CustomCollisionCsvWriter
    {
        public static void Write(string path, IReadOnlyList<CustomCollisionSample> samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var builder = new StringBuilder(1024 * 64);
            builder.AppendLine("case,scenario,objectCount,repetition,step,stepMs,pairChecks,collisions,triggerOverlaps,spatialCellsUsed");

            for (int i = 0; i < samples.Count; i++)
            {
                CustomCollisionSample sample = samples[i];
                builder.Append(Escape(sample.CaseName)).Append(',');
                builder.Append(sample.Scenario).Append(',');
                builder.Append(sample.ObjectCount).Append(',');
                builder.Append(sample.Repetition).Append(',');
                builder.Append(sample.Step).Append(',');
                builder.Append(sample.StepMs.ToString("F6", CultureInfo.InvariantCulture)).Append(',');
                builder.Append(sample.PairChecks).Append(',');
                builder.Append(sample.Collisions).Append(',');
                builder.Append(sample.TriggerOverlaps).Append(',');
                builder.AppendLine(sample.SpatialCellsUsed.ToString(CultureInfo.InvariantCulture));
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Contains(",") || value.Contains("\"") || value.Contains("\n")
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
