using System.Collections.Generic;
using System.IO;

namespace MxprjReader;

/// <summary>
/// Locates .mxprj files under the fixed folder layout used on a Maschine+ SD card:
/// "Native Instruments\Maschine 2\Projects" and "Native Instruments\Maschine 3\Projects".
/// The version is determined purely by which of those two folders a file was found under.
/// </summary>
public static class ProjectFinder
{
    public static IEnumerable<(string FilePath, MaschineVersion Version)> FindProjects(string driveRoot)
    {
        foreach (var (folderName, version) in new[]
        {
            ("Maschine 2", MaschineVersion.Maschine2),
            ("Maschine 3", MaschineVersion.Maschine3),
        })
        {
            var projectsFolder = Path.Combine(driveRoot, "Native Instruments", folderName, "Projects");
            if (!Directory.Exists(projectsFolder))
            {
                continue;
            }

            foreach (var filePath in Directory.GetFiles(projectsFolder, "*.mxprj", SearchOption.AllDirectories))
            {
                yield return (filePath, version);
            }
        }
    }
}
