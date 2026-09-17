namespace SwarmCatcher.Core;

public static class LessonPresenter
{
    public const string Briefing =
        "A swarm is looking for a new home. Swarming honey bees are generally docile, " +
        "but they can still sting: in real life, ask an experienced beekeeper for help. " +
        "A cardboard box works here, and any sufficiently large bee-tight container can hold a swarm.";

    public const string TemporaryBivouac =
        "The bees have formed a bivouac: a resting cluster. This stop may be temporary.";

    public const string FinalBivouac =
        "The swarm has settled into its final bivouac. Place the box below the cluster.";

    public const string BoxPlacement =
        "Move the box so its open top is beneath the bees, then choose the brush.";

    public const string Sweeping =
        "Sweep gently through the cluster. Bees over the opening will fall into the box.";

    public static string BuildResultRecap(CaptureResult result, bool experiencedTemporaryBivouac)
    {
        string outcome = result.Succeeded
            ? "You caught the swarm!"
            : "This swarm got away, but you can try again.";
        string queen = result.QueenCaptured
            ? "The queen is inside; she is needed for the new colony's success."
            : "The queen escaped. A new colony needs its queen to succeed.";
        string movement = experiencedTemporaryBivouac
            ? "You saw that a swarm may bivouac in several temporary places before settling."
            : "This swarm flew directly to its final bivouac, though other swarms may move several times.";

        return $"{outcome} You captured {result.CapturedPercentage:0.0}% of the bees. {queen} {movement}";
    }
}
