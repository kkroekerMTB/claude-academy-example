using SwarmCatcher.Core;
using System.Numerics;

namespace SwarmCatcher.Tests;

[TestClass]
public sealed class GameSessionTests
{
    [TestMethod]
    public void AdvanceMovesStartedSessionToBivouacUnlessPaused()
    {
        GameSession session = GameSession.Create(
            beeCount: 5_000,
            seed: 42,
            new SimulationBounds(1_920, 1_080));

        session.Start();
        session.Advance(TimeSpan.FromSeconds(3));
        session.Pause();
        session.Advance(TimeSpan.FromMinutes(1));

        Assert.AreEqual(GamePhase.Paused, session.Phase);

        session.Resume();
        session.Advance(TimeSpan.FromSeconds(3));

        Assert.AreEqual(GamePhase.Bivouacked, session.Phase);
        Assert.AreEqual(BeeStatus.Settled, session.Swarm.Bees[0].Status);
    }

    [TestMethod]
    public void RestartReturnsToBriefingWithANewSwarm()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        SwarmSimulation originalSwarm = session.Swarm;

        session.Restart();

        Assert.AreEqual(GamePhase.Briefing, session.Phase);
        Assert.AreNotSame(originalSwarm, session.Swarm);
        Assert.AreEqual(BeeStatus.Flying, session.Swarm.Bees[0].Status);
    }

    [TestMethod]
    public void SweepIntoBoxResolvesWithCapturedQueenAndMajority()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        session.ChooseBox();
        session.PlaceBox(new CaptureBox(left: 380, top: 260, width: 240, height: 180));
        session.ChooseBrush();

        session.Sweep(
            start: new Vector2(380, 180),
            end: new Vector2(620, 180),
            radius: 100);
        session.EndSweep();
        session.Advance(TimeSpan.FromSeconds(2));

        Assert.AreEqual(GamePhase.Resolved, session.Phase);
        Assert.IsNotNull(session.Result);
        Assert.IsTrue(session.Result.Value.QueenCaptured);
        Assert.IsGreaterThanOrEqualTo(50, session.Result.Value.CapturedPercentage);
        Assert.IsTrue(session.Result.Value.Succeeded);
    }
}
