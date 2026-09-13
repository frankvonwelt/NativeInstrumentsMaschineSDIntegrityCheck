using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MxprjReader;

public enum MaschineVersion
{
    Maschine2,
    Maschine3,
}

public enum SampleStatus
{
    Resolved,
    Missing,
    Unverifiable,
}

public sealed record SampleRow(SampleReference Sample, SampleStatus Status, string RelevantPath);

public sealed record ScannedProject(
    string FilePath,
    MaschineVersion Version,
    ScanStatus ScanStatus,
    IReadOnlyList<SampleRow> Samples)
{
    public string FileName => Path.GetFileName(FilePath);

    public int MissingCount => Samples.Count(s => s.Status == SampleStatus.Missing);

    public int UnverifiableCount => Samples.Count(s => s.Status == SampleStatus.Unverifiable);

    public bool IsPotentiallyBroken =>
        ScanStatus == ScanStatus.UnrecognizedFormat || MissingCount > 0 || UnverifiableCount > 0;

    public string StatusText
    {
        get
        {
            if (ScanStatus == ScanStatus.UnrecognizedFormat)
            {
                return "Unrecognized format";
            }

            var parts = new List<string>();
            if (MissingCount > 0)
            {
                parts.Add($"{MissingCount} missing");
            }

            if (UnverifiableCount > 0)
            {
                parts.Add($"{UnverifiableCount} unverifiable");
            }

            return parts.Count == 0 ? "OK" : string.Join(", ", parts);
        }
    }

    public static ScannedProject Analyze(string filePath, MaschineVersion version)
    {
        var bytes = File.ReadAllBytes(filePath);
        var scan = SampleReferenceScanner.Scan(bytes);
        var projectDirectory = Path.GetDirectoryName(filePath) ?? ".";

        var rows = new List<SampleRow>();
        foreach (var sample in scan.Samples)
        {
            if (sample.IsAbsolute)
            {
                rows.Add(new SampleRow(sample, SampleStatus.Unverifiable, sample.PathAsStored));
                continue;
            }

            var resolvedPath = Path.Combine(projectDirectory, sample.PathAsStored);
            var status = File.Exists(resolvedPath) ? SampleStatus.Resolved : SampleStatus.Missing;
            rows.Add(new SampleRow(sample, status, resolvedPath));
        }

        return new ScannedProject(filePath, version, scan.Status, rows);
    }
}
