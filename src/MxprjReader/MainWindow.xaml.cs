using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace MxprjReader;

public partial class MainWindow : Window
{
    private List<ScannedProject> _results = [];

    private sealed record DriveItem(string RootPath, string DisplayText)
    {
        public override string ToString() => DisplayText;
    }

    public MainWindow()
    {
        InitializeComponent();
        RefreshDrives();
    }

    private void RefreshDrivesButton_Click(object sender, RoutedEventArgs e) => RefreshDrives();

    private void RefreshDrives()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
            .Select(d => new DriveItem(d.Name, BuildDriveDisplayText(d)))
            .ToList();

        DriveComboBox.ItemsSource = drives;
        if (drives.Count > 0)
        {
            DriveComboBox.SelectedIndex = 0;
        }

        StatusTextBlock.Text = drives.Count == 0 ? "No removable drives found." : "";
    }

    private static string BuildDriveDisplayText(DriveInfo drive)
    {
        string? label = null;
        try
        {
            label = drive.VolumeLabel;
        }
        catch (IOException)
        {
        }

        return string.IsNullOrWhiteSpace(label) ? drive.Name : $"{drive.Name} ({label})";
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (DriveComboBox.SelectedItem is not DriveItem selectedDrive)
        {
            MessageBox.Show(this, "Select a drive first.", "No drive selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ScanButton.IsEnabled = false;
        StatusTextBlock.Text = "Scanning...";
        ProjectsDataGrid.ItemsSource = null;
        DetailsListBox.ItemsSource = null;

        try
        {
            _results = await Task.Run(() => ProjectFinder.FindProjects(selectedDrive.RootPath)
                .Select(p => ScannedProject.Analyze(p.FilePath, p.Version))
                .ToList());

            ProjectsDataGrid.ItemsSource = _results;

            var brokenCount = _results.Count(p => p.IsPotentiallyBroken);
            StatusTextBlock.Text = $"{_results.Count} project(s) scanned, {brokenCount} potentially broken.";
        }
        finally
        {
            ScanButton.IsEnabled = true;
        }
    }

    private void ProjectsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProjectsDataGrid.SelectedItem is not ScannedProject project)
        {
            DetailsListBox.ItemsSource = null;
            return;
        }

        if (project.ScanStatus == ScanStatus.UnrecognizedFormat)
        {
            DetailsListBox.ItemsSource = new[] { "File does not match the known .mxprj container signature." };
            return;
        }

        var lines = project.Samples
            .Where(s => s.Status != SampleStatus.Resolved)
            .Select(s => s.Status == SampleStatus.Missing
                ? $"MISSING: {s.RelevantPath}"
                : $"UNVERIFIABLE (absolute-path-only): {s.RelevantPath}")
            .ToList();

        DetailsListBox.ItemsSource = lines.Count == 0 ? new[] { "All samples resolved." } : lines;
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            MessageBox.Show(this, "Scan a drive first.", "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = "MxprjIntegrityReport.txt",
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, ReportWriter.BuildReport(_results));
        }
    }
}
