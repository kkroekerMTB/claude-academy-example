using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace SwarmCatcher.Performance;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += WindowLoaded;
        Closed += WindowClosed;
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Swarm.MetricsUpdated += UpdateMetrics;
        Swarm.Start();
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        Swarm.Stop();
        Swarm.MetricsUpdated -= UpdateMetrics;
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
}
