namespace SwarmCatcher.Performance;

public readonly record struct PerformanceMetrics(
    int BeeCount,
    double FramesPerSecond,
    double AverageFrameMilliseconds,
    double AllocatedKilobytesPerSecond);
