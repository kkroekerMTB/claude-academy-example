using System.Numerics;

namespace SwarmCatcher.Core;

public sealed class SwarmSimulation
{
    private const float MinimumSpeed = 95;
    private const float MaximumSpeed = 240;
    private const float NeighborhoodSize = 72;

    private readonly BeeState[] _bees;
    private readonly SimulationBounds _bounds;
    private readonly int[] _neighborhoodCounts;
    private readonly Vector2[] _neighborhoodPositions;
    private readonly Vector2[] _neighborhoodVelocities;
    private readonly int _neighborhoodColumns;
    private readonly int _neighborhoodRows;
    private float _elapsedSeconds;

    private SwarmSimulation(BeeState[] bees, SimulationBounds bounds)
    {
        _bees = bees;
        _bounds = bounds;
        _neighborhoodColumns = Math.Max(1, (int)MathF.Ceiling(bounds.Width / NeighborhoodSize));
        _neighborhoodRows = Math.Max(1, (int)MathF.Ceiling(bounds.Height / NeighborhoodSize));
        int neighborhoodCount = _neighborhoodColumns * _neighborhoodRows;
        _neighborhoodCounts = new int[neighborhoodCount];
        _neighborhoodPositions = new Vector2[neighborhoodCount];
        _neighborhoodVelocities = new Vector2[neighborhoodCount];
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
        var center = new Vector2(
            _bounds.Width * (0.5f + MathF.Sin(_elapsedSeconds * 0.31f) * 0.1f),
            _bounds.Height * (0.5f + MathF.Cos(_elapsedSeconds * 0.23f) * 0.07f));
        BuildNeighborhoods();

        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            Vector2 toCenter = center - bee.Position;
            Vector2 centerDirection = NormalizeOrZero(toCenter);
            var tangent = new Vector2(-centerDirection.Y, centerDirection.X);
            float phase = bee.Id * 0.7548777f + _elapsedSeconds * 3.2f;
            var flutter = new Vector2(MathF.Cos(phase), MathF.Sin(phase * 1.37f));
            GetNeighborhoodInfluence(bee, out Vector2 separation, out Vector2 alignment, out Vector2 cohesion);
            Vector2 acceleration = centerDirection * 30 + tangent * 20 + flutter * 44 +
                separation * 34 + alignment * 14 + cohesion * 18;
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

    internal void TakeFlight()
    {
        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            float angle = bee.Id * 2.3999632f;
            float speed = MinimumSpeed + bee.Id % (int)(MaximumSpeed - MinimumSpeed);
            _bees[index] = bee with
            {
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Status = BeeStatus.Flying,
            };
        }
    }

    internal bool HasFallingBees
    {
        get
        {
            foreach (BeeState bee in _bees)
            {
                if (bee.Status is BeeStatus.Falling or BeeStatus.Escaping)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal bool HasSettledBees
    {
        get
        {
            foreach (BeeState bee in _bees)
            {
                if (bee.Status == BeeStatus.Settled)
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
            if (bee.Status is not BeeStatus.Falling and not BeeStatus.Escaping)
            {
                continue;
            }

            Vector2 velocity = bee.Status == BeeStatus.Falling
                ? bee.Velocity + new Vector2(0, gravity * seconds)
                : bee.Velocity + new Vector2(0, -18 * seconds);
            Vector2 position = bee.Position + velocity * seconds;
            BeeStatus status = bee.Status;

            if (bee.Status == BeeStatus.Falling && bee.Position.Y <= box.Top && position.Y >= box.Top &&
                box.IsOverOpening(position.X))
            {
                position.Y = box.Top;
                velocity = Vector2.Zero;
                status = BeeStatus.Captured;
            }
            else if (bee.Status == BeeStatus.Falling && bee.Position.Y <= box.Top && position.Y >= box.Top)
            {
                float direction = position.X < box.Left + box.Width / 2 ? -1 : 1;
                float distanceToEdge = direction < 0 ? position.X : _bounds.Width - position.X;
                float escapeSpeed = MathF.Max(220 + bee.Id % 70, distanceToEdge / 1.25f);
                velocity = new Vector2(direction * escapeSpeed, -80 - bee.Id % 50);
                status = BeeStatus.Escaping;
            }
            else if (position.X < 0 || position.X > _bounds.Width ||
                position.Y < 0 || position.Y > _bounds.Height)
            {
                status = BeeStatus.Departed;
            }

            _bees[index] = bee with { Position = position, Velocity = velocity, Status = status };
        }
    }

    internal void ReleaseSettledBees()
    {
        for (int index = 0; index < _bees.Length; index++)
        {
            BeeState bee = _bees[index];
            if (bee.Status == BeeStatus.Settled)
            {
                float direction = bee.Position.X < _bounds.Width / 2 ? -1 : 1;
                float distanceToEdge = direction < 0 ? bee.Position.X : _bounds.Width - bee.Position.X;
                float speed = MathF.Max(220, distanceToEdge / 1.25f);
                _bees[index] = bee with
                {
                    Velocity = new Vector2(direction * speed, -40 - bee.Id % 80),
                    Status = BeeStatus.Escaping,
                };
            }
        }
    }

    private void BuildNeighborhoods()
    {
        Array.Clear(_neighborhoodCounts);
        Array.Clear(_neighborhoodPositions);
        Array.Clear(_neighborhoodVelocities);

        foreach (BeeState bee in _bees)
        {
            int cell = GetNeighborhoodIndex(bee.Position);
            _neighborhoodCounts[cell]++;
            _neighborhoodPositions[cell] += bee.Position;
            _neighborhoodVelocities[cell] += bee.Velocity;
        }
    }

    private void GetNeighborhoodInfluence(
        BeeState bee,
        out Vector2 separation,
        out Vector2 alignment,
        out Vector2 cohesion)
    {
        int column = Math.Clamp((int)(bee.Position.X / NeighborhoodSize), 0, _neighborhoodColumns - 1);
        int row = Math.Clamp((int)(bee.Position.Y / NeighborhoodSize), 0, _neighborhoodRows - 1);
        int count = 0;
        Vector2 positionTotal = Vector2.Zero;
        Vector2 velocityTotal = Vector2.Zero;

        for (int neighborRow = Math.Max(0, row - 1); neighborRow <= Math.Min(_neighborhoodRows - 1, row + 1); neighborRow++)
        {
            for (int neighborColumn = Math.Max(0, column - 1);
                neighborColumn <= Math.Min(_neighborhoodColumns - 1, column + 1);
                neighborColumn++)
            {
                int cell = neighborRow * _neighborhoodColumns + neighborColumn;
                count += _neighborhoodCounts[cell];
                positionTotal += _neighborhoodPositions[cell];
                velocityTotal += _neighborhoodVelocities[cell];
            }
        }

        if (count <= 1)
        {
            separation = Vector2.Zero;
            alignment = Vector2.Zero;
            cohesion = Vector2.Zero;
            return;
        }

        Vector2 averagePosition = (positionTotal - bee.Position) / (count - 1);
        Vector2 averageVelocity = (velocityTotal - bee.Velocity) / (count - 1);
        Vector2 offset = averagePosition - bee.Position;
        float distance = offset.Length();
        separation = distance < NeighborhoodSize * 0.42f ? -NormalizeOrZero(offset) : Vector2.Zero;
        alignment = NormalizeOrZero(averageVelocity - bee.Velocity);
        cohesion = distance >= NeighborhoodSize * 0.42f ? NormalizeOrZero(offset) : Vector2.Zero;
    }

    private int GetNeighborhoodIndex(Vector2 position)
    {
        int column = Math.Clamp((int)(position.X / NeighborhoodSize), 0, _neighborhoodColumns - 1);
        int row = Math.Clamp((int)(position.Y / NeighborhoodSize), 0, _neighborhoodRows - 1);
        return row * _neighborhoodColumns + column;
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
