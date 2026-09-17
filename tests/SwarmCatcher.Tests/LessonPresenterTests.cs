using SwarmCatcher.Core;

namespace SwarmCatcher.Tests;

[TestClass]
public sealed class LessonPresenterTests
{
    [TestMethod]
    public void BriefingQualifiesDocilityAndExplainsTheContainer()
    {
        string briefing = LessonPresenter.Briefing;

        StringAssert.Contains(briefing, "generally docile");
        StringAssert.Contains(briefing, "can still sting");
        StringAssert.Contains(briefing, "bee-tight container");
    }

    [TestMethod]
    public void ResultRecapExplainsWhyCapturingTheQueenMatters()
    {
        var result = new CaptureResult(3_000, 5_000, 60, true, true);

        string recap = LessonPresenter.BuildResultRecap(result, experiencedTemporaryBivouac: true);

        StringAssert.Contains(recap, "queen");
        StringAssert.Contains(recap, "new colony");
        StringAssert.Contains(recap, "several temporary places");
    }
}
