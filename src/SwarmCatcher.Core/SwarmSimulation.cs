using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class SwarmSimulation
{
    private const float MinimumSpeed = 45;
    private const float MaximumSpeed = 125;

    private readonly BeeState[] _bees;
    private readonly SimulationBounds _bounds;
    private float _elapsedSeconds;

    private SwarmSimulation(BeeState[] bees, SimulationBounds bounds)
    {
        _bees = bees;
        _bounds = bounds;
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
            var direction = new Vector2(
                random.NextSingle() * 2 - 1,
                random.NextSingle() * 2 - 1);
            if (direction.LengthSquared() < 0.001f)
            {
                direction = Vector2.UnitX;
            }

            var velocity = Vector2.Normalize(direction) *
                (MinimumSpeed + random.NextSingle() * (MaximumSpeed - MinimumSpeed));

            bees[id] = new BeeState(id, id == queenId, position, velocity);
        }

        return new SwarmSimulation(bees, bounds);
    }

    public void Advance(TimeSpan elapsed)
    {
        float seconds = (float)elapsed.TotalSeconds;
        ArgumentOutOfRangeException.ThrowIfNegative(seconds);

        _elapsedSeconds += seconds;
        var center = new Vector2(_bounds.Width / 2, _bounds.Height / 2);

        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            Vector2 toCenter = center - bee.Position;
            Vector2 centerDirection = NormalizeOrZero(toCenter);
            var tangent = new Vector2(-centerDirection.Y, centerDirection.X);
            float phase = bee.Id * 0.7548777f + _elapsedSeconds * 3.2f;
            var flutter = new Vector2(MathF.Cos(phase), MathF.Sin(phase * 1.37f));
            Vector2 acceleration = centerDirection * 18 + tangent * 12 + flutter * 28;
            Vector2 velocity = LimitSpeed(bee.Velocity + acceleration * seconds);
            Vector2 position = bee.Position + velocity * seconds;

            KeepInsideBounds(ref position, ref velocity);
            _bees[index] = bee with { Position = position, Velocity = velocity };
        }
    }

    private static Vector2 NormalizeOrZero(Vector2 value)
    {
        return value.LengthSquared() > 0.001f ? Vector2.Normalize(value) : Vector2.Zero;
    }

    private static Vector2 LimitSpeed(Vector2 velocity)
    {
        float speedSquared = velocity.LengthSquared();
        if (speedSquared < 0.001f)
        {
            return Vector2.UnitX * MinimumSpeed;
        }

        float speed = MathF.Sqrt(speedSquared);
        float limitedSpeed = Math.Clamp(speed, MinimumSpeed, MaximumSpeed);
        return velocity * (limitedSpeed / speed);
    }

    private void KeepInsideBounds(ref Vector2 position, ref Vector2 velocity)
    {
        if (position.X < 0 || position.X > _bounds.Width)
        {
            position.X = Math.Clamp(position.X, 0, _bounds.Width);
            velocity.X = -velocity.X;
        }

        if (position.Y < 0 || position.Y > _bounds.Height)
        {
            position.Y = Math.Clamp(position.Y, 0, _bounds.Height);
            velocity.Y = -velocity.Y;
        }
    }
}
