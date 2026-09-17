using SwarmCatcher.Core;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SwarmCatcher.App;

public sealed class GameSurface : FrameworkElement
{
    private static readonly TimeSpan SimulationStep = TimeSpan.FromSeconds(1.0 / 60);
    private static readonly Brush SkyBrush = Freeze(new LinearGradientBrush(
        Color.FromRgb(126, 190, 224), Color.FromRgb(225, 239, 205), 90));
    private static readonly Brush GrassBrush = Freeze(new SolidColorBrush(Color.FromRgb(78, 128, 66)));
    private static readonly Brush BeeBrush = Freeze(new SolidColorBrush(Color.FromRgb(38, 31, 19)));
    private static readonly Brush BeeHighlightBrush = Freeze(new SolidColorBrush(Color.FromRgb(244, 184, 42)));
    private static readonly Brush HighContrastBeeBrush = Freeze(new SolidColorBrush(Colors.White));
    private static readonly Brush BoxBrush = Freeze(new SolidColorBrush(Color.FromRgb(173, 112, 61)));
    private static readonly Brush BoxOpeningBrush = Freeze(new SolidColorBrush(Color.FromRgb(52, 35, 25)));
    private static readonly Brush BrushHandleBrush = Freeze(new SolidColorBrush(Color.FromRgb(224, 199, 145)));
    private static readonly Pen PlacementPen = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)), 2)
    {
        DashStyle = DashStyles.Dash,
    });
    private static readonly Pen HighContrastPen = Freeze(new Pen(Brushes.White, 4));
    private static readonly Pen BrushHandlePen = Freeze(new Pen(BrushHandleBrush, 10));

    private GameSession? _session;
    private TimeSpan _lastRenderingTime;
    private TimeSpan _accumulatedSimulationTime;
    private Point _pointerPosition;
    private Vector2 _lastSweepPoint;
    private bool _isSweeping;

    public GameSurface()
    {
        Focusable = true;
        ClipToBounds = true;
        Loaded += (_, _) => CompositionTarget.Rendering += RenderFrame;
        Unloaded += (_, _) => CompositionTarget.Rendering -= RenderFrame;
    }

    public event Action? StateChanged;

    public GamePhase Phase => _session?.Phase ?? GamePhase.Briefing;

    public CaptureResult? Result => _session?.Result;

    public bool ExperiencedTemporaryBivouac => _session?.ExperiencedTemporaryBivouac ?? false;

    public bool HasPlacedBox => _session?.Box is not null;

    public bool ReducedMotion { get; set; }

    public bool HighContrast { get; set; }

    public void StartGame()
    {
        EnsureSession();
        if (_session!.Phase == GamePhase.Briefing)
        {
            _session.Start();
            NotifyStateChanged();
        }
    }

    public void RestartGame()
    {
        EnsureSession();
        _session!.Restart();
        _session.Start();
        _isSweeping = false;
        ReleaseMouseCapture();
        NotifyStateChanged();
    }

    public void ChooseBox()
    {
        if (_session?.Phase == GamePhase.Bivouacked)
        {
            _session.ChooseBox();
            NotifyStateChanged();
        }
    }

    public void ChooseBrush()
    {
        if (_session?.Phase == GamePhase.BoxPlacement && _session.Box is not null)
        {
            _session.ChooseBrush();
            NotifyStateChanged();
        }
    }

    public void TogglePause()
    {
        if (_session is null || _session.Phase == GamePhase.Briefing || _session.Phase == GamePhase.Resolved)
        {
            return;
        }

        if (_session.Phase == GamePhase.Paused)
        {
            _session.Resume();
        }
        else
        {
            _session.Pause();
        }

        NotifyStateChanged();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        _pointerPosition = e.GetPosition(this);

        if (e.ChangedButton != MouseButton.Left || _session is null)
        {
            return;
        }

        if (_session.Phase == GamePhase.BoxPlacement)
        {
            PlaceBox(_pointerPosition);
        }
        else if (_session.Phase == GamePhase.Sweeping)
        {
            _isSweeping = true;
            _lastSweepPoint = ToVector(_pointerPosition);
            _session.Sweep(_lastSweepPoint, _lastSweepPoint, BrushRadius);
            CaptureMouse();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _pointerPosition = e.GetPosition(this);

        if (_session?.Phase == GamePhase.BoxPlacement && e.LeftButton == MouseButtonState.Pressed)
        {
            PlaceBox(_pointerPosition);
        }
        else if (_session?.Phase == GamePhase.Sweeping && _isSweeping)
        {
            Vector2 point = ToVector(_pointerPosition);
            _session.Sweep(_lastSweepPoint, point, BrushRadius);
            _lastSweepPoint = point;
        }

        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton == MouseButton.Left && _isSweeping && _session?.Phase == GamePhase.Sweeping)
        {
            _session.EndSweep();
            _isSweeping = false;
            ReleaseMouseCapture();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(HighContrast ? Brushes.Black : SkyBrush, null, new Rect(RenderSize));
        if (!HighContrast)
        {
            drawingContext.DrawRectangle(
                GrassBrush,
                null,
                new Rect(0, RenderSize.Height * 0.82, RenderSize.Width, RenderSize.Height * 0.18));
        }

        if (_session is null)
        {
            return;
        }

        DrawPlacementArea(drawingContext);
        DrawBees(drawingContext);
        DrawBox(drawingContext);
        DrawBrush(drawingContext);
    }

    private float BrushRadius => ReducedMotion ? 65 : 52;

    private void DrawBees(DrawingContext drawingContext)
    {
        foreach (BeeState bee in _session!.Swarm.Bees)
        {
            if (bee.Status is BeeStatus.Captured or BeeStatus.Departed)
            {
                continue;
            }

            double size = bee.IsQueen ? 3.2 : 1.7;
            Brush body = HighContrast ? HighContrastBeeBrush : BeeBrush;
            drawingContext.DrawEllipse(body, null, new Point(bee.Position.X, bee.Position.Y), size * 1.6, size);
            if (!HighContrast)
            {
                drawingContext.DrawEllipse(
                    BeeHighlightBrush,
                    null,
                    new Point(bee.Position.X - size * 0.35, bee.Position.Y),
                    size * 0.35,
                    size * 0.8);
            }
        }
    }

    private void DrawPlacementArea(DrawingContext drawingContext)
    {
        if (_session?.Phase != GamePhase.BoxPlacement)
        {
            return;
        }

        var area = new Rect(10, 110, Math.Max(0, ActualWidth - 20), Math.Max(0, ActualHeight - 230));
        drawingContext.DrawRoundedRectangle(null, HighContrast ? HighContrastPen : PlacementPen, area, 12, 12);
    }

    private void DrawBox(DrawingContext drawingContext)
    {
        if (_session?.Box is not CaptureBox box)
        {
            return;
        }

        var body = new Rect(box.Left, box.Top, box.Width, box.Height);
        drawingContext.DrawRoundedRectangle(BoxBrush, HighContrast ? HighContrastPen : null, body, 6, 6);

        bool closed = _session.Phase == GamePhase.Resolved;
        var opening = new Rect(box.Left + 10, box.Top - (closed ? 2 : 8), box.Width - 20, closed ? 8 : 16);
        drawingContext.DrawRoundedRectangle(BoxOpeningBrush, HighContrast ? HighContrastPen : null, opening, 4, 4);
    }

    private void DrawBrush(DrawingContext drawingContext)
    {
        if (_session?.Phase != GamePhase.Sweeping)
        {
            return;
        }

        var handleStart = new Point(_pointerPosition.X - 54, _pointerPosition.Y + 44);
        var handleEnd = new Point(_pointerPosition.X, _pointerPosition.Y);
        drawingContext.DrawLine(BrushHandlePen, handleStart, handleEnd);
        drawingContext.DrawEllipse(
            HighContrast ? Brushes.White : BeeHighlightBrush,
            HighContrast ? HighContrastPen : null,
            handleEnd,
            25,
            10);
    }

    private void PlaceBox(Point point)
    {
        const float width = 240;
        const float height = 180;
        float left = Math.Clamp((float)point.X - width / 2, 0, Math.Max(0, (float)ActualWidth - width));
        float top = Math.Clamp((float)point.Y, 0, Math.Max(0, (float)ActualHeight - height));
        _session!.PlaceBox(new CaptureBox(left, top, width, height));
        NotifyStateChanged();
    }

    private void EnsureSession()
    {
        if (_session is not null)
        {
            return;
        }

        var bounds = new SimulationBounds(
            Math.Max(640, (float)ActualWidth),
            Math.Max(480, (float)ActualHeight));
        _session = GameSession.Create(5_000, seed: 42, bounds);
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        if (_session is null || e is not RenderingEventArgs renderingEvent)
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
        GamePhase oldPhase = _session.Phase;

        while (_accumulatedSimulationTime >= SimulationStep)
        {
            _session.Advance(SimulationStep, ReducedMotion ? 0.5f : 1);
            _accumulatedSimulationTime -= SimulationStep;
        }

        if (_session.Phase != oldPhase)
        {
            NotifyStateChanged();
        }

        InvalidateVisual();
    }

    private void NotifyStateChanged()
    {
        InvalidateVisual();
        StateChanged?.Invoke();
    }

    private static Vector2 ToVector(Point point) => new((float)point.X, (float)point.Y);

    private static T Freeze<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
