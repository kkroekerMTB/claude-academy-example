using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class GameSession
{
    private static readonly TimeSpan SwarmingDuration = TimeSpan.FromSeconds(6);

    private readonly int _beeCount;
    private readonly SimulationBounds _bounds;
    private int _seed;
    private TimeSpan _phaseElapsed;
    private GamePhase _phaseBeforePause;

    private GameSession(int beeCount, int seed, SimulationBounds bounds)
    {
        _beeCount = beeCount;
        _seed = seed;
        _bounds = bounds;
        Swarm = SwarmSimulation.Create(beeCount, seed, bounds);
    }

    public GamePhase Phase { get; private set; } = GamePhase.Briefing;

    public SwarmSimulation Swarm { get; private set; }

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

    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);

        if (Phase != GamePhase.Swarming)
        {
            return;
        }

        Swarm.Advance(elapsed);
        _phaseElapsed += elapsed;

        if (_phaseElapsed >= SwarmingDuration)
        {
            var anchor = new Vector2(_bounds.Width * 0.62f, _bounds.Height * 0.3f);
            Swarm.SettleAt(anchor);
            Phase = GamePhase.Bivouacked;
            _phaseElapsed = TimeSpan.Zero;
        }
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
    }

    private void RequirePhase(GamePhase expected)
    {
        if (Phase != expected)
        {
            throw new InvalidOperationException($"Expected game phase {expected}, but the session is in {Phase}.");
        }
    }
}
