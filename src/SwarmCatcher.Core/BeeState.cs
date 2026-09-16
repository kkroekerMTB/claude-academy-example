using System.Numerics;

namespace SwarmCatcher.Core;

public readonly record struct BeeState(
    int Id,
    bool IsQueen,
    Vector2 Position,
    Vector2 Velocity);
