namespace SwarmCatcher.Performance;

public sealed record PerformanceReport(
    DateTimeOffset RecordedAtUtc,
    string OperatingSystem,
    int LogicalProcessorCount,
    long AvailableMemoryBytes,
    double Width,
    double Height,
    int BeeCount,
    double DurationSeconds,
    int RenderedFrames,
    double FramesPerSecond,
    double AverageFrameMilliseconds,
    double P95FrameMilliseconds,
    double P99FrameMilliseconds,
    double AllocatedKilobytesPerSecond,
    string Renderer);
