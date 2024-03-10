namespace Atlas.Auto.Tests.Tests.RepeatSearch;

/// <summary>
/// Tests that cover exception paths of Atlas repeat search.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category($"{nameof(RepeatSearch_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class RepeatSearch_ExceptionPathTests : RepeatSearchTestBase
{
    public RepeatSearch_ExceptionPathTests() : base(nameof(RepeatSearch_ExceptionPathTests))
    {
    }

    [Test]
    public async Task RepeatSearch_MissingRequiredInfo_ReturnsValidationError()
    {
        var test = GetRepeatSearchTestServices(nameof(RepeatSearch_MissingRequiredInfo_ReturnsValidationError));
        const string testDescription = "Repeat search with missing required information";
        test.Logger.LogStart(testDescription);
        await test.Steps.RepeatRequestMissingRequiredInfoShouldReturnValidationErrors();
        test.Logger.LogCompletion(testDescription);
    }
}