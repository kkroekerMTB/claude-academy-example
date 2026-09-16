namespace SwarmCatcher.Core;

public readonly record struct SimulationBounds
{
    public SimulationBounds(float width, float height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
    }

    public float Width { get; }

    public float Height { get; }
}
