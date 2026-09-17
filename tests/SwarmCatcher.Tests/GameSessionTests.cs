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
    public void OddSeedSwarmMovesOnFromTemporaryBivouacBeforeSettling()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 43,
            new SimulationBounds(800, 600));

        session.Start();
        session.Advance(TimeSpan.FromSeconds(3));

        Assert.AreEqual(GamePhase.TemporaryBivouac, session.Phase);
        Assert.AreEqual(BeeStatus.Settled, session.Swarm.Bees[0].Status);

        session.Advance(TimeSpan.FromSeconds(2));

        Assert.AreEqual(GamePhase.Swarming, session.Phase);
        Assert.AreEqual(BeeStatus.Flying, session.Swarm.Bees[0].Status);

        session.Advance(TimeSpan.FromSeconds(3));

        Assert.AreEqual(GamePhase.Bivouacked, session.Phase);
        Assert.AreEqual(BeeStatus.Settled, session.Swarm.Bees[0].Status);
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
        Assert.IsTrue(session.IsBoxClosed);
        session.Advance(TimeSpan.FromSeconds(2));

        Assert.AreEqual(GamePhase.Resolved, session.Phase);
        Assert.IsNotNull(session.Result);
        Assert.IsTrue(session.Result.Value.QueenCaptured);
        Assert.IsGreaterThanOrEqualTo(50, session.Result.Value.CapturedPercentage);
        Assert.IsTrue(session.Result.Value.Succeeded);
    }

    [TestMethod]
    public void BeesMissingTheBoxRecoverAndFlyOutOfView()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        session.ChooseBox();
        session.PlaceBox(new CaptureBox(left: 0, top: 340, width: 100, height: 180));
        session.ChooseBrush();
        session.Sweep(new Vector2(420, 300), new Vector2(570, 300), radius: 100);
        session.EndSweep();

        session.Advance(TimeSpan.FromSeconds(0.5));

        Assert.IsTrue(session.Swarm.Bees.ToArray().Any(bee => bee.Status == BeeStatus.Escaping));

        session.Advance(TimeSpan.FromSeconds(2));

        Assert.AreEqual(GamePhase.Resolved, session.Phase);
        Assert.AreEqual(0, session.Result?.CapturedCount);
        Assert.IsFalse(session.Result?.Succeeded);
    }

    [TestMethod]
    public void BoxCanBeRepositionedBeforeTheBrushIsChosen()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        session.ChooseBox();

        session.PlaceBox(new CaptureBox(left: 100, top: 300, width: 200, height: 160));
        var finalBox = new CaptureBox(left: 350, top: 320, width: 200, height: 160);
        session.PlaceBox(finalBox);

        Assert.AreEqual(finalBox, session.Box);
    }

    [TestMethod]
    public void BrushedBeesFallWhileTheSweepIsStillInProgress()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        session.ChooseBox();
        session.PlaceBox(new CaptureBox(left: 380, top: 360, width: 240, height: 180));
        session.ChooseBrush();
        session.Sweep(new Vector2(380, 300), new Vector2(620, 300), radius: 100);
        BeeState fallingBee = session.Swarm.Bees.ToArray().First(bee => bee.Status == BeeStatus.Falling);

        session.Advance(TimeSpan.FromSeconds(0.1));

        BeeState advancedBee = session.Swarm.Bees[fallingBee.Id];
        Assert.AreEqual(GamePhase.Sweeping, session.Phase);
        Assert.AreNotEqual(fallingBee.Position, advancedBee.Position);
    }

    [TestMethod]
    public void BoxClosesAutomaticallyWhenNoActiveBeesRemain()
    {
        GameSession session = GameSession.Create(
            beeCount: 100,
            seed: 42,
            new SimulationBounds(800, 600));
        session.Start();
        session.Advance(TimeSpan.FromSeconds(6));
        session.ChooseBox();
        session.PlaceBox(new CaptureBox(left: 380, top: 360, width: 240, height: 180));
        session.ChooseBrush();
        session.Sweep(new Vector2(360, 180), new Vector2(640, 180), radius: 120);

        for (int frame = 0; frame < 240 && session.Phase == GamePhase.Sweeping; frame++)
        {
            session.Advance(TimeSpan.FromSeconds(1.0 / 60));
        }

        Assert.IsTrue(session.IsBoxClosed);
        Assert.AreEqual(GamePhase.Resolved, session.Phase);
    }
}
