using SwarmCatcher.Core;
using System.Numerics;

namespace SwarmCatcher.Tests;

[TestClass]
public sealed class OutcomeEvaluatorTests
{
    [TestMethod]
    public void EvaluateSucceedsAtFiftyPercentWhenQueenIsCaptured()
    {
        BeeState[] bees = CreateBees(beeCount: 10, capturedBeeCount: 5, queenId: 2);

        CaptureResult result = OutcomeEvaluator.Evaluate(bees);

        Assert.AreEqual(5, result.CapturedCount);
        Assert.AreEqual(10, result.TotalCount);
        Assert.AreEqual(50, result.CapturedPercentage);
        Assert.IsTrue(result.QueenCaptured);
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public void EvaluateFailsWhenQueenIsNotCaptured()
    {
        BeeState[] bees = CreateBees(beeCount: 10, capturedBeeCount: 8, queenId: 9);

        CaptureResult result = OutcomeEvaluator.Evaluate(bees);

        Assert.AreEqual(80, result.CapturedPercentage);
        Assert.IsFalse(result.QueenCaptured);
        Assert.IsFalse(result.Succeeded);
    }

    private static BeeState[] CreateBees(int beeCount, int capturedBeeCount, int queenId)
    {
        var bees = new BeeState[beeCount];
        for (int id = 0; id < beeCount; id++)
        {
            BeeStatus status = id < capturedBeeCount ? BeeStatus.Captured : BeeStatus.Departed;
            bees[id] = new BeeState(id, id == queenId, Vector2.Zero, Vector2.Zero, status);
        }

        return bees;
    }
}
