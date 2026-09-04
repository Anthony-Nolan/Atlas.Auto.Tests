using System.Reflection;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Debug.Client.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Atlas.Auto.Tests.DependencyInjection;

internal static class ServiceConfiguration
{
    internal static IServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(new NUnitSink())
                .CreateLogger();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        services.RegisterDebugClients(configuration,
            ("DonorImport", typeof(IDonorImportFunctionsClient), typeof(DonorImportFunctionsClient)),
            ("MatchingAlgorithm", typeof(IMatchingAlgorithmFunctionsClient), typeof(MatchingAlgorithmFunctionsClient)),
            ("TopLevel", typeof(ITopLevelFunctionsClient), typeof(TopLevelFunctionsClient)),
            ("PublicApi", typeof(IPublicApiFunctionsClient), typeof(PublicApiFunctionsClient)),
            ("RepeatSearch", typeof(IRepeatSearchFunctionsClient), typeof(RepeatSearchFunctionsClient)));

        services.AddSingleton(configuration.GetSection("Retry").Get<RetrySettings>() ?? new RetrySettings());

        services.AddSingleton<PollyRetry>();

        return services.BuildServiceProvider();
    }
}
