using SwarmCatcher.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SwarmCatcher.App;

public sealed partial class MainWindow : Window, IDisposable
{
    private readonly BuzzAudio _buzzAudio = new();
    private bool _disposed;

    public MainWindow()
    {
        InitializeComponent();
        Game.StateChanged += UpdatePresentation;
        Closed += (_, _) => Dispose();
        UpdatePresentation();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buzzAudio.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void Start_Click(object sender, RoutedEventArgs e) => Game.StartGame();

    private void Box_Click(object sender, RoutedEventArgs e) => Game.ChooseBox();

    private void Brush_Click(object sender, RoutedEventArgs e) => Game.ChooseBrush();

    private void Restart_Click(object sender, RoutedEventArgs e) => Game.RestartGame();

    private void Pause_Click(object sender, RoutedEventArgs e) => Game.TogglePause();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.S:
                Game.StartGame();
                break;
            case Key.D1:
            case Key.NumPad1:
                Game.ChooseBox();
                break;
            case Key.D2:
            case Key.NumPad2:
                Game.ChooseBrush();
                break;
            case Key.P:
            case Key.Escape:
                Game.TogglePause();
                break;
            case Key.R:
                Game.RestartGame();
                break;
            case Key.X:
                Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void HighContrast_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            Game.HighContrast = HighContrastCheck.IsChecked == true;
            Game.InvalidateVisual();
        }
    }

    private void ReducedMotion_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            Game.ReducedMotion = ReducedMotionCheck.IsChecked == true;
        }
    }

    private void Audio_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            _buzzAudio.Volume = MuteCheck.IsChecked == true ? 0 : VolumeSlider.Value;
        }
    }

    private void TextScale_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && TextScale.SelectedItem is ComboBoxItem item &&
            double.TryParse(item.Tag?.ToString(), out double size))
        {
            FontSize = size;
        }
    }

    private void UpdatePresentation()
    {
        GamePhase phase = Game.Phase;
        LessonText.Text = phase switch
        {
            GamePhase.Briefing => LessonPresenter.Briefing,
            GamePhase.Swarming => "The swarm is flying together while each bee moves independently. Watch where it settles.",
            GamePhase.TemporaryBivouac => LessonPresenter.TemporaryBivouac,
            GamePhase.Bivouacked => LessonPresenter.FinalBivouac,
            GamePhase.BoxPlacement => LessonPresenter.BoxPlacement,
            GamePhase.Sweeping => LessonPresenter.Sweeping,
            GamePhase.Paused => "The game is paused. The swarm will wait for you.",
            GamePhase.Resolved => "The catch is complete.",
            _ => string.Empty,
        };

        StartButton.IsEnabled = phase == GamePhase.Briefing;
        BoxButton.IsEnabled = phase == GamePhase.Bivouacked;
        BrushButton.IsEnabled = phase == GamePhase.BoxPlacement && Game.HasPlacedBox;
        PauseButton.IsEnabled = phase is not GamePhase.Briefing and not GamePhase.Resolved;
        PauseButton.Content = phase == GamePhase.Paused ? "Resume [P]" : "Pause [P]";
        PausePanel.Visibility = phase == GamePhase.Paused ? Visibility.Visible : Visibility.Collapsed;

        ResultPanel.Visibility = phase == GamePhase.Resolved ? Visibility.Visible : Visibility.Collapsed;
        if (Game.Result is CaptureResult result)
        {
            ResultTitle.Text = result.Succeeded ? "Swarm caught!" : "The swarm escaped";
            ResultText.Text = LessonPresenter.BuildResultRecap(result, Game.ExperiencedTemporaryBivouac);
        }

        if (phase == GamePhase.Swarming)
        {
            _buzzAudio.Play();
        }
        else
        {
            _buzzAudio.Stop();
        }
    }
}
