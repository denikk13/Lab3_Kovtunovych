using System.Text;

namespace lab_1.Views
{
    public partial class LogsPage : ContentPage
    {
        private string _activeLevel = "ALL";
        private List<string> _allLines = new();

        public LogsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadLogs();
        }

        private string GetLogsDirectory()
        {
            return Path.Combine(FileSystem.AppDataDirectory, "logs");
        }

        private void LoadLogs()
        {
            try
            {
                var logDir = GetLogsDirectory();
                if (!Directory.Exists(logDir))
                {
                    _allLines = new List<string>();
                    StatusLabel.Text = "No log directory yet";
                    ApplyFilter();
                    return;
                }

                var latestFile = Directory
                    .GetFiles(logDir, "app-*.log")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(latestFile) || !File.Exists(latestFile))
                {
                    _allLines = new List<string>();
                    StatusLabel.Text = "No log files found";
                    ApplyFilter();
                    return;
                }

                using var stream = new FileStream(
                    latestFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var rawText = reader.ReadToEnd();
                _allLines = SplitIntoLogEntries(rawText);
                StatusLabel.Text = Path.GetFileName(latestFile);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                LogsEditor.Text = $"Failed to read logs: {ex.Message}";
            }
        }

        private static List<string> SplitIntoLogEntries(string rawText)
        {
            var entries = new List<string>();
            if (string.IsNullOrEmpty(rawText))
                return entries;

            var lines = rawText.Split('\n');
            var current = new StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                bool startsNewEntry =
                    line.Length >= 23 &&
                    char.IsDigit(line[0]) &&
                    line[4] == '-' &&
                    line[7] == '-' &&
                    line[10] == ' ';

                if (startsNewEntry && current.Length > 0)
                {
                    entries.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0)
                    current.AppendLine();
                current.Append(line);
            }

            if (current.Length > 0)
                entries.Add(current.ToString());

            return entries;
        }

        private void ApplyFilter()
        {
            IEnumerable<string> filtered = _allLines;

            if (_activeLevel != "ALL")
            {
                var marker = $"[{_activeLevel}]";
                filtered = _allLines.Where(entry => entry.Contains(marker, StringComparison.OrdinalIgnoreCase));
            }

            var result = string.Join(Environment.NewLine, filtered);
            LogsEditor.Text = string.IsNullOrWhiteSpace(result)
                ? $"No entries for level {_activeLevel}"
                : result;
        }

        private void OnFilterClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && !string.IsNullOrWhiteSpace(btn.Text))
            {
                _activeLevel = btn.Text.Trim().ToUpperInvariant();
                ApplyFilter();
            }
        }

        private void OnRefreshClicked(object? sender, EventArgs e)
        {
            LoadLogs();
        }

        private async void OnClearClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Clear logs",
                "Clear current log display and remove older log files?",
                "Yes",
                "No");

            if (!confirm)
                return;

            try
            {
                var logDir = GetLogsDirectory();
                if (Directory.Exists(logDir))
                {
                    var files = Directory.GetFiles(logDir, "app-*.log")
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToList();

                    foreach (var oldFile in files.Skip(1))
                    {
                        File.Delete(oldFile);
                    }
                }

                _allLines.Clear();
                LogsEditor.Text = "Log view cleared.";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}