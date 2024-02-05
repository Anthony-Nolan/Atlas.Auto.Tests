using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.Auto.Tests.DependencyInjection
{
    /// <summary>
    /// Reference: https://github.com/Anthony-Nolan/Atlas/blob/master/Atlas.Common/Utils/Extensions/DependencyInjectionUtils.cs
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Uses Microsoft DI & IConfiguration frameworks to register that a settings object can be found in the App Settings, under a particular key.
        /// Expected to be used primarily in a set with <see cref="OptionsReaderFor{T}"/>.
        /// </summary>
        /// <typeparam name="T">Settings Object to be populated</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="sectionName">Settings key from which to read the Settings</param>
        public static void RegisterAsOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }

        /// <summary>
        /// Builds an accessor which pulls the defined Settings object out of the Microsoft DI & Configuration framework.
        /// Note that this is expected to be used primarily in a pair with <see cref="RegisterAsOptions{T}"/>, to pass a
        /// previously registered option on to the project dependency that needs it.
        /// </summary>
        /// <typeparam name="T">Settings Object to be accessed</typeparam>
        public static Func<IServiceProvider, T> OptionsReaderFor<T>() where T : class, new()
        {
            return sp => sp.GetService<IOptions<T>>().Value;
        }

        /// <summary>
        /// Resolves the service from the <paramref name="provider"/> else throws an error if the service is not found.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T ResolveServiceOrThrow<T>(this IServiceProvider provider) where T : class
        {
            var service = provider.GetService<T>();
            return service ?? throw new InvalidOperationException($"Unable to resolve {typeof(T).Name} from the service provider. Make sure it's registered correctly.");
        }
    }
}
