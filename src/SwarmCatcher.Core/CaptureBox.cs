namespace SwarmCatcher.Core;

public readonly record struct CaptureBox
{
    public CaptureBox(float left, float top, float width, float height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public float Left { get; }

    public float Top { get; }

    public float Width { get; }

    public float Height { get; }

    public float Right => Left + Width;

    public float Bottom => Top + Height;

    internal bool IsInside(SimulationBounds bounds)
    {
        return Right <= bounds.Width && Bottom <= bounds.Height;
    }

    internal bool IsOverOpening(float x)
    {
        float rim = MathF.Min(10, Width * 0.08f);
        return x >= Left + rim && x <= Right - rim;
    }
}
