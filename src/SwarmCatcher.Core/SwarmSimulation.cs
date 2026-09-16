using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class SwarmSimulation
{
    private readonly BeeState[] _bees;

    private SwarmSimulation(BeeState[] bees)
    {
        _bees = bees;
    }

    public int BeeCount => _bees.Length;

    public ReadOnlySpan<BeeState> Bees => _bees;

    public static SwarmSimulation Create(int beeCount, int seed, SimulationBounds bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beeCount);

        var random = new Random(seed);
        var bees = new BeeState[beeCount];
        int queenId = random.Next(beeCount);
        var center = new Vector2(bounds.Width / 2, bounds.Height / 2);
        float radius = MathF.Min(bounds.Width, bounds.Height) * 0.18f;

        for (int id = 0; id < bees.Length; id++)
        {
            float angle = random.NextSingle() * MathF.Tau;
            float distance = MathF.Sqrt(random.NextSingle()) * radius;
            var position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            var velocity = new Vector2(
                random.NextSingle() * 2 - 1,
                random.NextSingle() * 2 - 1);

            bees[id] = new BeeState(id, id == queenId, position, velocity);
        }

        return new SwarmSimulation(bees);
    }
}
