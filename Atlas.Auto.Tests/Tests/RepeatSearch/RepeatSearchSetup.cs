using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

/// <summary>
/// Sets the short repeat search wait of <see cref="FunctionAppTestSettings"/> (QA-2921) only when the run has repeat search tests,
/// and restores it after them. NUnit runs it before any fixture of this namespace, and no repeat search test starts
/// until the function app has restarted.
/// </summary>
/// <remarks>
/// It is set on a shared environment: while it is on, any repeat search waits for its original results for 1 minute, not 24 h.
/// </remarks>
[SetUpFixture]
internal class RepeatSearchSetup
{
    private const string RepeatSearchApp = "RepeatSearch";

    [OneTimeSetUp]
    public async Task SetUp()
    {
        await new FunctionAppTestSettingsSwitcher(TestBase.Provider).SetTestValues(RepeatSearchApp);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await new FunctionAppTestSettingsSwitcher(TestBase.Provider).Restore(RepeatSearchApp);
    }
}
