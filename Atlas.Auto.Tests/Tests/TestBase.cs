using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.DonorImport.FileSchema.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.Tests;

[SetUpFixture]
internal class AssemblySetup
{
    private const string DonorImportApp = "DonorImport";

    /// <remarks>
    /// Removes the test donors earlier runs left behind, a failed attempt of a rerun job included. It removes every test
    /// donor, so no two test runs may target one Atlas at the same time: the pipeline runs them one after another.
    /// </remarks>
    [OneTimeSetUp]
    public async Task SetUp()
    {
        var importDonorTypeNames = Enum.GetNames(typeof(ImportDonorType)).ToHashSet();
        Assert.That(
            Enum.GetNames(typeof(DatabaseDonorType)),
            Is.SubsetOf(importDonorTypeNames),
            "DatabaseDonorType names must match ImportDonorType for ToString() comparison");
        Assert.That(
            Enum.GetNames(typeof(MatchingDonorType)),
            Is.SubsetOf(importDonorTypeNames),
            "MatchingDonorType names must match ImportDonorType for ToString() comparison");

        // The active matching database may be paused; wake it before anything queries it
        await new MatchingDatabaseWaker(TestBase.Provider).WakeUp();
        await new TestDonorDeleter(TestBase.Provider).DeleteDonors();

        // Set before any test: changing it restarts the donor import function, and tests of every area import donors
        await new FunctionAppTestSettingsSwitcher(TestBase.Provider).SetTestValues(DonorImportApp);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        try
        {
            // Restores the deployed values the setup kept on the function app
            await new FunctionAppTestSettingsSwitcher(TestBase.Provider).Restore(DonorImportApp);
        }
        finally
        {
            (TestBase.Provider as IDisposable)?.Dispose();
        }
    }
}

[Parallelizable(ParallelScope.All)]
internal abstract class TestBase
{
    private static readonly Lazy<IServiceProvider> LazyProvider = new(ServiceConfiguration.CreateProvider);
    internal static IServiceProvider Provider => LazyProvider.Value;

    protected readonly string TestFixtureName;

    protected TestBase(string testFixtureName)
    {
        TestFixtureName = testFixtureName;
    }

    protected ILogger BuildTestLogger(string context) =>
        Provider.GetRequiredService<ILoggerFactory>().CreateLogger(context);
}
