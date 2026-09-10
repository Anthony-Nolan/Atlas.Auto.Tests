using Atlas.Auto.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.Tests;

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
