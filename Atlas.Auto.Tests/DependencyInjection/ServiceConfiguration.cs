using System.Reflection;
using Atlas.Debug.Client;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.Debug.Client.Models.MatchingAlgorithm;
using Atlas.Debug.Client.Models.SupportMessages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Auto.Tests.DependencyInjection.Utils;

namespace Atlas.Auto.Tests.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        internal static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            services.SetUpConfiguration();
            services.RegisterSettings();

            services.RegisterDebugClients(
                OptionsReaderFor<DonorImportHttpFunctionSettings>(),
                OptionsReaderFor<MatchingAlgorithmHttpFunctionSettings>(),
                OptionsReaderFor<SupportMessageHttpFunctionSettings>());

            return services.BuildServiceProvider();
        }

        private static void SetUpConfiguration(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<DonorImportHttpFunctionSettings>("DonorImport");
            services.RegisterAsOptions<MatchingAlgorithmHttpFunctionSettings>("MatchingAlgorithm");
            services.RegisterAsOptions<SupportMessageHttpFunctionSettings>("SupportMessages");
        }
    }
}
