namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
[Category($"{nameof(RepeatSearch_ExceptionPathTests)}")]
internal class RepeatSearch_ExceptionPathTests : RepeatSearchTestBase
{
    public RepeatSearch_ExceptionPathTests() : base(nameof(RepeatSearch_ExceptionPathTests))
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
