using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.DonorImport.FileSchema.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.Tests;

[SetUpFixture]
internal class AssemblySetup
{
    [OneTimeSetUp]
    public void SetUp()
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
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        (TestBase.Provider as IDisposable)?.Dispose();
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
