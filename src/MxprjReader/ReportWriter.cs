using System.Text;

namespace MxprjReader;

public static class ReportWriter
{
    public static string BuildReport(IEnumerable<ScannedProject> projects)
    {
        var report = new StringBuilder();

        foreach (var project in projects)
        {
            report.AppendLine($"{project.FilePath} [{project.Version}]: {project.StatusText} ({project.Samples.Count} sample reference(s) found)");

            foreach (var row in project.Samples.Where(r => r.Status != SampleStatus.Resolved))
            {
                if (row.Status == SampleStatus.Missing)
                {
                    report.AppendLine($"  MISSING: {row.RelevantPath}");
                    report.AppendLine($"    stale absolute base in file: {row.Sample.StaleAbsoluteBase}");
                }
                else
                {
                    report.AppendLine($"  UNVERIFIABLE (absolute-path-only, no relative field stored): {row.RelevantPath}");
                }
            }

            report.AppendLine();
        }

        return report.ToString();
    }
}
