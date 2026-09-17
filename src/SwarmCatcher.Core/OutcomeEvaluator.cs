namespace SwarmCatcher.Core;

public static class OutcomeEvaluator
{
    public static CaptureResult Evaluate(ReadOnlySpan<BeeState> bees)
    {
        if (bees.IsEmpty)
        {
            throw new ArgumentException("A capture outcome requires at least one bee.", nameof(bees));
        }

        int capturedCount = 0;
        bool queenCaptured = false;

        foreach (BeeState bee in bees)
        {
            if (bee.Status != BeeStatus.Captured)
            {
                continue;
            }

            capturedCount++;
            queenCaptured |= bee.IsQueen;
        }

        double capturedPercentage = capturedCount * 100d / bees.Length;
        return new CaptureResult(
            capturedCount,
            bees.Length,
            capturedPercentage,
            queenCaptured,
            capturedPercentage >= 50 && queenCaptured);
    }
}
