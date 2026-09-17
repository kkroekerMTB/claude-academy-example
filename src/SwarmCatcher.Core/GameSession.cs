using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class GameSession
{
    private static readonly TimeSpan DirectFlightDuration = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan FlightLegDuration = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TemporaryBivouacDuration = TimeSpan.FromSeconds(1.5);

    private readonly int _beeCount;
    private readonly SimulationBounds _bounds;
    private int _seed;
    private TimeSpan _phaseElapsed;
    private GamePhase _phaseBeforePause;
    private CaptureBox? _box;
    private bool _sweepEnded;
    private bool _temporaryBivouacCompleted;

    private GameSession(int beeCount, int seed, SimulationBounds bounds)
    {
        _beeCount = beeCount;
        _seed = seed;
        _bounds = bounds;
        Swarm = SwarmSimulation.Create(beeCount, seed, bounds);
    }

    public GamePhase Phase { get; private set; } = GamePhase.Briefing;

    public SwarmSimulation Swarm { get; private set; }

    public CaptureBox? Box => _box;

    public CaptureResult? Result { get; private set; }

    public bool ExperiencedTemporaryBivouac { get; private set; }

    public static GameSession Create(int beeCount, int seed, SimulationBounds bounds)
    {
        return new GameSession(beeCount, seed, bounds);
    }

    public void Start()
    {
        RequirePhase(GamePhase.Briefing);
        Phase = GamePhase.Swarming;
        _phaseElapsed = TimeSpan.Zero;
    }

    public void Advance(TimeSpan elapsed, float flightSpeedScale = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(flightSpeedScale, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(flightSpeedScale, 1);

        if (Phase == GamePhase.Swarming)
        {
            AdvanceSwarming(elapsed, flightSpeedScale);
        }
        else if (Phase == GamePhase.TemporaryBivouac)
        {
            AdvanceTemporaryBivouac(elapsed);
        }
        else if (Phase == GamePhase.Sweeping && _sweepEnded)
        {
            AdvanceFallingBees(elapsed);
        }
    }

    public void ChooseBox()
    {
        RequirePhase(GamePhase.Bivouacked);
        Phase = GamePhase.BoxPlacement;
    }

    public void PlaceBox(CaptureBox box)
    {
        RequirePhase(GamePhase.BoxPlacement);
        if (!box.IsInside(_bounds))
        {
            throw new ArgumentOutOfRangeException(nameof(box), "The box must fit inside the game surface.");
        }

        _box = box;
    }

    public void ChooseBrush()
    {
        RequirePhase(GamePhase.BoxPlacement);
        if (_box is null)
        {
            throw new InvalidOperationException("Place the box before choosing the brush.");
        }

        Phase = GamePhase.Sweeping;
    }

    public void Sweep(Vector2 start, Vector2 end, float radius)
    {
        RequirePhase(GamePhase.Sweeping);
        if (_sweepEnded)
        {
            throw new InvalidOperationException("The sweep has already ended.");
        }

        Swarm.ApplyBrushStroke(start, end, radius);
    }

    public void EndSweep()
    {
        RequirePhase(GamePhase.Sweeping);
        _sweepEnded = true;
        _phaseElapsed = TimeSpan.Zero;
    }

    private void AdvanceSwarming(TimeSpan elapsed, float flightSpeedScale)
    {
        Swarm.Advance(elapsed * flightSpeedScale);
        _phaseElapsed += elapsed;

        bool usesTemporaryBivouac = (_seed & 1) == 1;
        if (usesTemporaryBivouac && !_temporaryBivouacCompleted &&
            _phaseElapsed >= FlightLegDuration)
        {
            Swarm.SettleAt(new Vector2(_bounds.Width * 0.36f, _bounds.Height * 0.34f));
            ExperiencedTemporaryBivouac = true;
            Phase = GamePhase.TemporaryBivouac;
            _phaseElapsed = TimeSpan.Zero;
            return;
        }

        TimeSpan finalFlightDuration = usesTemporaryBivouac
            ? FlightLegDuration
            : DirectFlightDuration;
        if (_phaseElapsed >= finalFlightDuration)
        {
            var anchor = new Vector2(_bounds.Width * 0.62f, _bounds.Height * 0.3f);
            Swarm.SettleAt(anchor);
            Phase = GamePhase.Bivouacked;
            _phaseElapsed = TimeSpan.Zero;
        }
    }

    private void AdvanceTemporaryBivouac(TimeSpan elapsed)
    {
        _phaseElapsed += elapsed;
        if (_phaseElapsed < TemporaryBivouacDuration)
        {
            return;
        }

        Swarm.TakeFlight();
        _temporaryBivouacCompleted = true;
        Phase = GamePhase.Swarming;
        _phaseElapsed = TimeSpan.Zero;
    }

    private void AdvanceFallingBees(TimeSpan elapsed)
    {
        CaptureBox box = _box ?? throw new InvalidOperationException("A sweep requires a placed box.");
        Swarm.AdvanceFalling(elapsed, box);
        _phaseElapsed += elapsed;

        if (Swarm.HasFallingBees && _phaseElapsed < TimeSpan.FromSeconds(2))
        {
            return;
        }

        Swarm.DepartUncapturedBees();
        Result = OutcomeEvaluator.Evaluate(Swarm.Bees);
        Phase = GamePhase.Resolved;
    }

    public void Pause()
    {
        if (Phase == GamePhase.Paused)
        {
            return;
        }

        _phaseBeforePause = Phase;
        Phase = GamePhase.Paused;
    }

    public void Resume()
    {
        RequirePhase(GamePhase.Paused);
        Phase = _phaseBeforePause;
    }

    public void Restart()
    {
        _seed++;
        Swarm = SwarmSimulation.Create(_beeCount, _seed, _bounds);
        Phase = GamePhase.Briefing;
        _phaseElapsed = TimeSpan.Zero;
        _box = null;
        _sweepEnded = false;
        _temporaryBivouacCompleted = false;
        Result = null;
        ExperiencedTemporaryBivouac = false;
    }

    private void RequirePhase(GamePhase expected)
    {
        if (Phase != expected)
        {
            throw new InvalidOperationException($"Expected game phase {expected}, but the session is in {Phase}.");
        }
    }
}
