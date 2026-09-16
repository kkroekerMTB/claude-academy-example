using SwarmCatcher.Core;
using System.Numerics;

namespace SwarmCatcher.Tests;

[TestClass]
public sealed class SwarmSimulationTests
{
    [TestMethod]
    public void CreateProducesRequestedBeeCountWithExactlyOneQueen()
    {
        SwarmSimulation simulation = SwarmSimulation.Create(
            beeCount: 5_000,
            seed: 42,
            new SimulationBounds(width: 1_920, height: 1_080));

        var identities = new HashSet<int>();
        int queenCount = 0;

        foreach (BeeState bee in simulation.Bees)
        {
            identities.Add(bee.Id);
            queenCount += bee.IsQueen ? 1 : 0;
        }

        Assert.AreEqual(5_000, simulation.BeeCount);
        Assert.HasCount(5_000, identities);
        Assert.AreEqual(1, queenCount);
    }

    [TestMethod]
    public void AdvanceMovesBeesWithinTheSimulationBounds()
    {
        var bounds = new SimulationBounds(width: 1_920, height: 1_080);
        SwarmSimulation simulation = SwarmSimulation.Create(beeCount: 5_000, seed: 42, bounds);
        Vector2[] startingPositions = simulation.Bees.ToArray().Select(bee => bee.Position).ToArray();

        simulation.Advance(TimeSpan.FromSeconds(1.0 / 60));

        int movedBeeCount = 0;
        for (int index = 0; index < simulation.BeeCount; index++)
        {
            Vector2 position = simulation.Bees[index].Position;
            if (position != startingPositions[index])
            {
                movedBeeCount++;
            }

            Assert.IsTrue(float.IsFinite(position.X));
            Assert.IsTrue(float.IsFinite(position.Y));
            Assert.IsGreaterThanOrEqualTo(0, position.X);
            Assert.IsLessThanOrEqualTo(bounds.Width, position.X);
            Assert.IsGreaterThanOrEqualTo(0, position.Y);
            Assert.IsLessThanOrEqualTo(bounds.Height, position.Y);
        }

        Assert.AreEqual(simulation.BeeCount, movedBeeCount);
    }
}
