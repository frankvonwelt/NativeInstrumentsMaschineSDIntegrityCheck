using System.Text;

namespace MxprjReader;

/// <summary>
/// One sample reference as stored in the project file. Two storage shapes have been observed
/// (see docs/mxprj-format-notes.md):
///   - a short project-relative path plus a separately-stored stale absolute base directory
///     (<see cref="StaleAbsoluteBase"/> is set), or
///   - a single, already-absolute path with no separate relative field at all
///     (<see cref="PathAsStored"/> itself is absolute, <see cref="StaleAbsoluteBase"/> is null).
/// We don't know how Maschine resolves the second shape, so it's reported as its own category
/// rather than guessed at.
/// </summary>
public sealed record SampleReference(string PathAsStored, string? StaleAbsoluteBase)
{
    public bool IsAbsolute => IsAbsolutePath(PathAsStored);

    public static bool IsAbsolutePath(string path) =>
        path.StartsWith('/') || (path.Length >= 2 && path[1] == ':');
}

public sealed record ScanResult(ScanStatus Status, IReadOnlyList<SampleReference> Samples);

public enum ScanStatus
{
    Ok,
    UnrecognizedFormat,
}

/// <summary>
/// Heuristic byte-level scanner for .mxprj files. The container format is not fully decoded
/// (see docs/mxprj-format-notes.md), so this looks for the known "2SAM" signature and then
/// pulls out Pascal-style (1-byte length prefix) strings, pairing each sample filename with
/// the stale absolute path that follows it in the byte stream, where one is present.
/// </summary>
public static class SampleReferenceScanner
{
    private const string ContainerSignature = "2SAMJORP";

    private static readonly string[] SampleExtensions =
    [
        ".wav", ".aif", ".aiff", ".flac", ".ogg",
    ];

    public static ScanResult Scan(byte[] fileBytes)
    {
        if (!ContainsSignature(fileBytes, ContainerSignature))
        {
            return new ScanResult(ScanStatus.UnrecognizedFormat, []);
        }

        var strings = ExtractPascalStrings(fileBytes);
        var samples = new List<SampleReference>();

        for (var i = 0; i < strings.Count; i++)
        {
            var candidate = strings[i];
            if (!IsSamplePath(candidate))
            {
                continue;
            }

            if (SampleReference.IsAbsolutePath(candidate))
            {
                samples.Add(new SampleReference(candidate, null));
                continue;
            }

            string? stalePairedBase = null;
            if (i + 1 < strings.Count)
            {
                var next = strings[i + 1];
                if (SampleReference.IsAbsolutePath(next) && !IsSamplePath(next))
                {
                    stalePairedBase = next;
                }
            }

            samples.Add(new SampleReference(candidate, stalePairedBase));
        }

        return new ScanResult(ScanStatus.Ok, samples);
    }

    private static bool ContainsSignature(byte[] data, string signature)
    {
        var needle = Encoding.ASCII.GetBytes(signature);
        for (var i = 0; i <= data.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSamplePath(string candidate) =>
        SampleExtensions.Any(ext => candidate.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Scans the buffer for 1-byte-length-prefixed printable-ASCII runs (Pascal strings).
    /// </summary>
    private static List<string> ExtractPascalStrings(byte[] data)
    {
        var result = new List<string>();
        var i = 0;

        while (i < data.Length)
        {
            var length = data[i];
            if (length >= 4 && length <= 200 && i + 1 + length <= data.Length && IsPrintableRun(data, i + 1, length))
            {
                result.Add(Encoding.ASCII.GetString(data, i + 1, length));
                i += 1 + length;
            }
            else
            {
                i++;
            }
        }

        return result;
    }

    private static bool IsPrintableRun(byte[] data, int offset, int length)
    {
        for (var i = 0; i < length; i++)
        {
            var b = data[offset + i];
            if (b < 0x20 || b > 0x7E)
            {
                return false;
            }
        }

        return true;
    }
}
