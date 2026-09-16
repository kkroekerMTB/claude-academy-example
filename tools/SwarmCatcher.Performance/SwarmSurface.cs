using SwarmCatcher.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace SwarmCatcher.Performance;

public sealed class SwarmSurface : FrameworkElement
{
    private static readonly Brush BackgroundBrush = CreateFrozenBrush(Color.FromRgb(16, 24, 32));
    private static readonly Brush BeeBrush = CreateFrozenBrush(Color.FromRgb(246, 190, 43));
    private static readonly Brush QueenBrush = CreateFrozenBrush(Color.FromRgb(255, 111, 45));
    private static readonly TimeSpan SimulationStep = TimeSpan.FromSeconds(1.0 / 60);

    private readonly Stopwatch _clock = new();
    private SwarmSimulation? _simulation;
    private TimeSpan _lastRenderingTime;
    private TimeSpan _accumulatedSimulationTime;
    private double _measurementStartedAt;
    private long _measurementStartedBytes;
    private int _renderedFrames;

    public event Action<PerformanceMetrics>? MetricsUpdated;

    public void Start()
    {
        if (_clock.IsRunning)
        {
            return;
        }

        var bounds = new SimulationBounds(
            width: Math.Max(1, (float)ActualWidth),
            height: Math.Max(1, (float)ActualHeight));
        _simulation = SwarmSimulation.Create(beeCount: 5_000, seed: 42, bounds);
        _lastRenderingTime = TimeSpan.Zero;
        _accumulatedSimulationTime = TimeSpan.Zero;
        _measurementStartedAt = 0;
        _measurementStartedBytes = GC.GetTotalAllocatedBytes(precise: false);
        _renderedFrames = 0;
        _clock.Restart();
        CompositionTarget.Rendering += RenderFrame;
    }

    public void Stop()
    {
        CompositionTarget.Rendering -= RenderFrame;
        _clock.Stop();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));

        if (_simulation is null)
        {
            return;
        }

        foreach (BeeState bee in _simulation.Bees)
        {
            double radius = bee.IsQueen ? 3.5 : 1.6;
            Brush brush = bee.IsQueen ? QueenBrush : BeeBrush;
            drawingContext.DrawEllipse(
                brush,
                null,
                new Point(bee.Position.X, bee.Position.Y),
                radius * 1.45,
                radius);
        }
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        if (_simulation is null || e is not RenderingEventArgs renderingEvent)
        {
            return;
        }

        if (_lastRenderingTime == TimeSpan.Zero)
        {
            _lastRenderingTime = renderingEvent.RenderingTime;
            return;
        }

        TimeSpan elapsed = renderingEvent.RenderingTime - _lastRenderingTime;
        _lastRenderingTime = renderingEvent.RenderingTime;
        _accumulatedSimulationTime += elapsed > TimeSpan.FromMilliseconds(100)
            ? TimeSpan.FromMilliseconds(100)
            : elapsed;

        while (_accumulatedSimulationTime >= SimulationStep)
        {
            _simulation.Advance(SimulationStep);
            _accumulatedSimulationTime -= SimulationStep;
        }

        InvalidateVisual();
        RecordFrame();
    }

    private void RecordFrame()
    {
        _renderedFrames++;
        double elapsedSeconds = _clock.Elapsed.TotalSeconds - _measurementStartedAt;
        if (elapsedSeconds < 1)
        {
            return;
        }

        long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false) - _measurementStartedBytes;
        double framesPerSecond = _renderedFrames / elapsedSeconds;
        MetricsUpdated?.Invoke(new PerformanceMetrics(
            _simulation?.BeeCount ?? 0,
            framesPerSecond,
            1_000 / framesPerSecond,
            allocatedBytes / 1_024d / elapsedSeconds));

        _measurementStartedAt = _clock.Elapsed.TotalSeconds;
        _measurementStartedBytes = GC.GetTotalAllocatedBytes(precise: false);
        _renderedFrames = 0;
    }
}
