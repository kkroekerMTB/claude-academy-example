using SwarmCatcher.Core;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace SwarmCatcher.App;

public sealed class SwarmRenderer
{
    private static readonly Brush BeeBrush = Freeze(new SolidColorBrush(Color.FromRgb(246, 190, 43)));
    private static readonly Brush QueenBrush = Freeze(new SolidColorBrush(Color.FromRgb(255, 111, 45)));
    private static readonly Brush HighContrastBeeBrush = Freeze(new SolidColorBrush(Colors.White));

    private readonly StreamGeometry _beeGeometry = new() { FillRule = FillRule.Nonzero };

    public void DrawBees(
        DrawingContext drawingContext,
        ReadOnlySpan<BeeState> bees,
        ReadOnlySpan<BeeState> previousBees,
        double interpolation,
        bool highContrast)
    {
        bool canInterpolate = previousBees.Length == bees.Length;
        float amount = (float)Math.Clamp(interpolation, 0, 1);
        bool hasQueen = false;
        Vector2 queenPosition = Vector2.Zero;

        using (StreamGeometryContext geometry = _beeGeometry.Open())
        {
            for (int index = 0; index < bees.Length; index++)
            {
                BeeState bee = bees[index];
                if (bee.Status is BeeStatus.Captured or BeeStatus.Departed)
                {
                    continue;
                }

                Vector2 position = canInterpolate && previousBees[index].Status == bee.Status
                    ? Vector2.Lerp(previousBees[index].Position, bee.Position, amount)
                    : bee.Position;
                if (bee.IsQueen)
                {
                    hasQueen = true;
                    queenPosition = position;
                    continue;
                }

                AddBeeDiamond(geometry, position, radius: 1.8);
            }
        }

        drawingContext.DrawGeometry(highContrast ? HighContrastBeeBrush : BeeBrush, null, _beeGeometry);
        if (hasQueen)
        {
            drawingContext.DrawEllipse(
                highContrast ? HighContrastBeeBrush : QueenBrush,
                null,
                new Point(queenPosition.X, queenPosition.Y),
                5.2,
                3.2);
        }
    }

    private static void AddBeeDiamond(StreamGeometryContext geometry, Vector2 position, double radius)
    {
        geometry.BeginFigure(new Point(position.X - radius * 1.6, position.Y), isFilled: true, isClosed: true);
        geometry.LineTo(new Point(position.X, position.Y - radius), isStroked: true, isSmoothJoin: false);
        geometry.LineTo(new Point(position.X + radius * 1.6, position.Y), isStroked: true, isSmoothJoin: false);
        geometry.LineTo(new Point(position.X, position.Y + radius), isStroked: true, isSmoothJoin: false);
    }

    private static T Freeze<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
