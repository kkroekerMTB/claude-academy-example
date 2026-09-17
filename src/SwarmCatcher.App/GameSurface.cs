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
    private static readonly Brush BoxBrush = Freeze(new SolidColorBrush(Color.FromRgb(173, 112, 61)));
    private static readonly Brush BoxOpeningBrush = Freeze(new SolidColorBrush(Color.FromRgb(52, 35, 25)));
    private static readonly Brush BrushHandleBrush = Freeze(new SolidColorBrush(Color.FromRgb(224, 199, 145)));
    private static readonly Brush BrushHeadBrush = Freeze(new SolidColorBrush(Color.FromRgb(244, 184, 42)));
    private static readonly Pen PlacementPen = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)), 2)
    {
        DashStyle = DashStyles.Dash,
    });
    private static readonly Pen HighContrastPen = Freeze(new Pen(Brushes.White, 4));
    private static readonly Pen InvalidPlacementPen = Freeze(new Pen(Brushes.Red, 5));
    private static readonly Pen BrushHandlePen = Freeze(new Pen(BrushHandleBrush, 10));

    private readonly SwarmRenderer _swarmRenderer = new();
    private GameSession? _session;
    private TimeSpan _lastRenderingTime;
    private TimeSpan _accumulatedSimulationTime;
    private Point _pointerPosition;
    private Vector2 _lastSweepPoint;
    private bool _isSweeping;
    private bool _invalidPlacement;
    private BeeState[]? _previousFrame;

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

    public bool InvalidPlacement => _invalidPlacement;

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
        _invalidPlacement = false;
        CancelPointerInteraction();
        CapturePreviousFrame();
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
            CancelPointerInteraction();
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
        else if (_session?.Phase == GamePhase.Sweeping && _isSweeping &&
            e.LeftButton == MouseButtonState.Pressed)
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

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _isSweeping = false;
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
        double interpolation = _accumulatedSimulationTime.TotalSeconds / SimulationStep.TotalSeconds;
        _swarmRenderer.DrawBees(
            drawingContext,
            _session.Swarm.Bees,
            _previousFrame,
            interpolation,
            HighContrast);
        DrawBox(drawingContext);
        DrawBrush(drawingContext);
    }

    private float BrushRadius => ReducedMotion ? 65 : 52;

    private void DrawPlacementArea(DrawingContext drawingContext)
    {
        if (_session?.Phase != GamePhase.BoxPlacement)
        {
            return;
        }

        Rect area = GetBoxPlacementArea();
        Pen areaPen = _invalidPlacement ? InvalidPlacementPen : HighContrast ? HighContrastPen : PlacementPen;
        drawingContext.DrawRoundedRectangle(null, areaPen, area, 12, 12);
        if (_invalidPlacement)
        {
            drawingContext.DrawLine(
                InvalidPlacementPen,
                new Point(_pointerPosition.X - 16, _pointerPosition.Y - 16),
                new Point(_pointerPosition.X + 16, _pointerPosition.Y + 16));
            drawingContext.DrawLine(
                InvalidPlacementPen,
                new Point(_pointerPosition.X + 16, _pointerPosition.Y - 16),
                new Point(_pointerPosition.X - 16, _pointerPosition.Y + 16));
        }
    }

    private void DrawBox(DrawingContext drawingContext)
    {
        if (_session?.Box is not CaptureBox box)
        {
            return;
        }

        var body = new Rect(box.Left, box.Top, box.Width, box.Height);
        drawingContext.DrawRoundedRectangle(BoxBrush, HighContrast ? HighContrastPen : null, body, 6, 6);

        bool closed = _session.IsBoxClosed;
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
            HighContrast ? Brushes.White : BrushHeadBrush,
            HighContrast ? HighContrastPen : null,
            handleEnd,
            25,
            10);
    }

    private void PlaceBox(Point point)
    {
        const float width = 240;
        const float height = 180;
        Rect area = GetBoxPlacementArea();
        float left = (float)point.X - width / 2;
        float top = (float)point.Y;
        _invalidPlacement = left < area.Left || left + width > area.Right ||
            top < area.Top || top + height > area.Bottom;
        if (_invalidPlacement)
        {
            NotifyStateChanged();
            return;
        }

        _session!.PlaceBox(new CaptureBox(left, top, width, height));
        NotifyStateChanged();
    }

    private Rect GetBoxPlacementArea()
    {
        const double horizontalMargin = 20;
        const double bottomMargin = 120;
        double clusterBottom = 120;
        if (_session is not null)
        {
            foreach (BeeState bee in _session.Swarm.Bees)
            {
                if (bee.Status == BeeStatus.Settled)
                {
                    clusterBottom = Math.Max(clusterBottom, bee.Position.Y + 15);
                }
            }
        }

        double bottom = Math.Max(180, ActualHeight - bottomMargin);
        double top = Math.Min(clusterBottom, bottom - 180);
        return new Rect(
            horizontalMargin,
            top,
            Math.Max(240, ActualWidth - horizontalMargin * 2),
            bottom - top);
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
        CapturePreviousFrame();
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
            CapturePreviousFrame();
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

    private void CapturePreviousFrame()
    {
        if (_session is null)
        {
            return;
        }

        _previousFrame ??= new BeeState[_session.Swarm.BeeCount];
        _session.Swarm.Bees.CopyTo(_previousFrame);
    }

    private void CancelPointerInteraction()
    {
        _isSweeping = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }

    private static Vector2 ToVector(Point point) => new((float)point.X, (float)point.Y);

    private static T Freeze<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
