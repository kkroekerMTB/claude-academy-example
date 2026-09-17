using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class SwarmSimulation
{
    private const float MinimumSpeed = 95;
    private const float MaximumSpeed = 240;

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
            Vector2 acceleration = centerDirection * 32 + tangent * 22 + flutter * 48;
            Vector2 velocity = LimitSpeed(bee.Velocity + acceleration * seconds);
            Vector2 position = bee.Position + velocity * seconds;

            KeepInsideBounds(ref position, ref velocity);
            _bees[index] = bee with { Position = position, Velocity = velocity };
        }
    }

    internal void SettleAt(Vector2 anchor)
    {
        const float goldenAngle = 2.3999632f;
        float clusterRadius = MathF.Min(_bounds.Width, _bounds.Height) * 0.1f;

        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            float distance = MathF.Sqrt((index + 0.5f) / _bees.Length) * clusterRadius;
            float angle = index * goldenAngle;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            _bees[index] = bee with
            {
                Position = anchor + offset,
                Velocity = Vector2.Zero,
                Status = BeeStatus.Settled,
            };
        }
    }

    internal bool HasFallingBees
    {
        get
        {
            foreach (BeeState bee in _bees)
            {
                if (bee.Status == BeeStatus.Falling)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal void ApplyBrushStroke(Vector2 start, Vector2 end, float radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        float radiusSquared = radius * radius;

        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            if (bee.Status != BeeStatus.Settled ||
                DistanceSquaredToSegment(bee.Position, start, end) > radiusSquared)
            {
                continue;
            }

            float lateralVelocity = ((bee.Id * 37 % 49) - 24) * 0.9f;
            float downwardVelocity = 85 + bee.Id % 35;
            _bees[index] = bee with
            {
                Velocity = new Vector2(lateralVelocity, downwardVelocity),
                Status = BeeStatus.Falling,
            };
        }
    }

    internal void AdvanceFalling(TimeSpan elapsed, CaptureBox box)
    {
        float seconds = (float)elapsed.TotalSeconds;
        const float gravity = 420;

        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            if (bee.Status != BeeStatus.Falling)
            {
                continue;
            }

            Vector2 velocity = bee.Velocity + new Vector2(0, gravity * seconds);
            Vector2 position = bee.Position + velocity * seconds;
            BeeStatus status = bee.Status;

            if (bee.Position.Y <= box.Top && position.Y >= box.Top && box.IsOverOpening(position.X))
            {
                position.Y = box.Top;
                velocity = Vector2.Zero;
                status = BeeStatus.Captured;
            }
            else if (position.Y > _bounds.Height)
            {
                status = BeeStatus.Departed;
            }

            _bees[index] = bee with { Position = position, Velocity = velocity, Status = status };
        }
    }

    internal void DepartUncapturedBees()
    {
        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            if (bee.Status is BeeStatus.Settled or BeeStatus.Falling)
            {
                _bees[index] = bee with { Status = BeeStatus.Departed };
            }
        }
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLengthSquared = segment.LengthSquared();
        if (segmentLengthSquared < 0.001f)
        {
            return Vector2.DistanceSquared(point, start);
        }

        float position = Vector2.Dot(point - start, segment) / segmentLengthSquared;
        Vector2 closest = start + segment * Math.Clamp(position, 0, 1);
        return Vector2.DistanceSquared(point, closest);
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
