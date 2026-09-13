# MxprjReader: WPF app (pivot from console)

## Context

The original console tool (see `docs/mxprj-format-notes.md` for the file-format findings it's
built on) took an arbitrary folder path on the command line. In real use, projects live on a
Maschine+ SD card with a fixed layout:

```
<drive>\Native Instruments\Maschine 2\Projects\...\*.mxprj
<drive>\Native Instruments\Maschine 3\Projects\...\*.mxprj
```

This is also the answer to the earlier "how do we tell Maschine 2 from Maschine 3 projects"
question we couldn't resolve from the file bytes alone: version is determined by **which of the
two `Projects` folders a file was found under**, not by parsing anything inside the file. The
user's long-term interest is Maschine 3, but both are shown.

The tool is being turned into a WPF app so a user can just plug in an SD card, pick it from a
list, and see which projects are broken — no command line involved.

## Scope

- WPF app (net10.0, `net10.0-windows`, `UseWPF`), replacing the console entry point in place.
  `SampleReferenceScanner.cs` (the byte-level scanner) is reused unchanged.
- **No MVVM** — plain code-behind, per project convention. Scan results are plain C# objects
  assigned directly to `ItemsSource`; no `INotifyPropertyChanged`/commands/bindings machinery.
- Main window:
  - A drive picker listing available drives (`DriveType.Removable`, `IsReady`) with a Refresh
    button, since the target is an SD card in a reader.
  - A "Scan" button. Scanning looks for `Native Instruments\Maschine 2\Projects` and
    `Native Instruments\Maschine 3\Projects` under the selected drive's root and recurses into
    subfolders under each for `*.mxprj` files. A `Projects` folder that doesn't exist on a given
    drive is skipped silently (not every SD card will have both versions present).
  - Scanning runs on a background thread (`Task.Run`) so the UI doesn't freeze; the button is
    disabled while a scan is in progress.
  - A list of scanned projects (file name, Maschine version, status: `OK` / `N missing` /
    `N unverifiable` / `Unrecognized format`), and a details panel that shows the per-sample
    missing/unverifiable lines for whichever project is selected — same information the console
    report printed, just interactive instead of a flat file.
  - A "list-level" status marker per project (broken vs. not) so the whole result set can be
    scanned at a glance without opening each project's details.
  - An "Export Report..." button that opens a `SaveFileDialog` and writes the same plain-text
    report format the console tool produced (one block per project: file path, status, then any
    missing/unverifiable sample lines), for every project in the current results.
- Out of scope for this pass: fixing/rewriting project files, anything for drives that aren't
  plugged in yet (no auto-refresh/watch).

## Implementation notes

- `MxprjReader.csproj`: change `OutputType` to `WinExe`, add `<UseWPF>true</UseWPF>`, target
  `net10.0-windows`. Remove `Program.cs`; add `App.xaml`/`App.xaml.cs` and
  `MainWindow.xaml`/`MainWindow.xaml.cs`.
- New small type, e.g. `ScannedProject` (file path, display name, `MaschineVersion` enum
  {`Maschine2`, `Maschine3`}, the `ScanResult` from `SampleReferenceScanner`, and the
  resolved/missing/unverifiable breakdown) — this is just a plain data holder used to populate
  the list and the details panel, not a ViewModel layer.
- Drive scan logic (code-behind, e.g. in `MainWindow.xaml.cs` or a small `ProjectFinder` helper):
  for the selected drive root, check
  `Path.Combine(drive, "Native Instruments", "Maschine 2", "Projects")` and the `Maschine 3`
  equivalent; for each that exists, `Directory.GetFiles(path, "*.mxprj", SearchOption.AllDirectories)`.
- Reuse the existing missing/unverifiable classification logic from `Program.cs` (resolving each
  `SampleReference` against the project's own folder) — move it next to `ScannedProject` since
  `Program.cs` goes away.

## Verification

- Run the app, confirm it lists local drives (a USB stick or SD reader is the realistic test;
  short of that, temporarily pointing the "drive" picker at `C:\` to confirm the folder-walk logic
  finds `ExampleProjects`-style content is an acceptable substitute, then revert).
- Since none of the actual `ExampleProjects` files sit under a
  `Native Instruments\Maschine 2\Projects` layout, do a one-off manual check: copy/point the
  scanner at a temp folder shaped like `Native Instruments\Maschine 3\Projects\...` containing a
  copy of e.g. `AlmostHome.mxprj` and confirm it's found, tagged `Maschine 3`, and reported with
  8 missing samples — matching what the console tool already confirmed.
