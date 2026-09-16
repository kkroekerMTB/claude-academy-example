using SwarmCatcher.Core;

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
}
