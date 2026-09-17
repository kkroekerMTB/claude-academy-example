namespace SwarmCatcher.Core;

public readonly record struct CaptureResult(
    int CapturedCount,
    int TotalCount,
    double CapturedPercentage,
    bool QueenCaptured,
    bool Succeeded);
