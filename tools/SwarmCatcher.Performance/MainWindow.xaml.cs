using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SwarmCatcher.Performance;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions ReportJsonOptions = new() { WriteIndented = true };

    private readonly string? _reportPath;
    private readonly DispatcherTimer? _completionTimer;

    public MainWindow()
    {
        InitializeComponent();
        (_reportPath, TimeSpan? duration) = ReadOptions(Environment.GetCommandLineArgs());
        if (duration is not null)
        {
            _completionTimer = new DispatcherTimer { Interval = duration.Value };
            _completionTimer.Tick += (_, _) => Close();
        }

        Loaded += WindowLoaded;
        Closed += WindowClosed;
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Swarm.MetricsUpdated += UpdateMetrics;
        Swarm.Start();
        _completionTimer?.Start();
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        _completionTimer?.Stop();
        PerformanceReport report = Swarm.StopAndCreateReport();
        Swarm.MetricsUpdated -= UpdateMetrics;

        if (_reportPath is not null)
        {
            string fullPath = Path.GetFullPath(_reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(
                fullPath,
                JsonSerializer.Serialize(report, ReportJsonOptions));
        }
    }

    private void UpdateMetrics(PerformanceMetrics metrics)
    {
        MetricsText.Text = string.Format(
            CultureInfo.InvariantCulture,
            "{0:N0} bees\n{1:N1} rendered FPS\n{2:N2} ms average frame\n{3:N1} KB allocated/sec",
            metrics.BeeCount,
            metrics.FramesPerSecond,
            metrics.AverageFrameMilliseconds,
            metrics.AllocatedKilobytesPerSecond);
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private static (string? ReportPath, TimeSpan? Duration) ReadOptions(string[] arguments)
    {
        string? reportPath = null;
        TimeSpan? duration = null;

        for (int index = 1; index < arguments.Length; index++)
        {
            if (arguments[index] == "--output" && index + 1 < arguments.Length)
            {
                reportPath = arguments[++index];
            }
            else if (arguments[index] == "--duration-minutes" && index + 1 < arguments.Length &&
                double.TryParse(arguments[++index], CultureInfo.InvariantCulture, out double minutes) &&
                minutes > 0)
            {
                duration = TimeSpan.FromMinutes(minutes);
            }
        }

        return (reportPath, duration);
    }
}
