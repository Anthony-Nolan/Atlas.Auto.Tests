using Atlas.Auto.Tests.TestHelpers.SourceData;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
[Category($"{TestConstants.RepeatSearchTestTag}_{nameof(RepeatSearchExceptionPathTests)}")]
internal class RepeatSearchExceptionPathTests : RepeatSearchTestBase
{
    public RepeatSearchExceptionPathTests() : base(nameof(RepeatSearchExceptionPathTests))
    {
    }

    [Test]
    public async Task RepeatSearch_MissingRequiredInfo_ReturnsValidationError()
    {
        var steps = GetRepeatSearchTestSteps(nameof(RepeatSearch_MissingRequiredInfo_ReturnsValidationError));
        const string testDescription = "Repeat search with missing required information";
        steps.Logger.LogStart(testDescription);
        await steps.RepeatRequestMissingRequiredInfoShouldReturnValidationErrors();
        steps.Logger.LogCompletion(testDescription);
    }
}
