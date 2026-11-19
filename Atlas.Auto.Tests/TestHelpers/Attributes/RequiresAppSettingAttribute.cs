using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.Tests;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;

namespace Atlas.Auto.Tests.TestHelpers.Attributes;

/// <summary>
/// Marks a test that needs a function app setting of <see cref="FunctionAppTestSettings"/>, which a [OneTimeSetUp] sets for the run,
/// e.g. <c>[RequiresAppSetting(FunctionAppTestSettings.NotifyOnUntrackedDonorDeletion)]</c>.
/// Shows up as the "RequiresAppSetting" property in test results and can be used in filters:
/// <c>--filter "RequiresAppSetting=RepeatSearch:MaxWaitForOriginalResultsMinutes"</c>.
/// The test is skipped when the run may not change app settings (Azure:ChangeFunctionAppSettings is false),
/// and fails, before it does any work, when the [OneTimeSetUp] could not set them.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal class RequiresAppSettingAttribute(string settingKey) : PropertyAttribute(PropertyName, settingKey), ITestAction
{
    public const string PropertyName = "RequiresAppSetting";

    /// <summary>The app setting the test needs, as in <see cref="FunctionAppTestSettings"/>.</summary>
    public string SettingKey { get; } = settingKey;

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        if (!TestBase.Provider.GetRequiredService<AzureResourceSettings>().ChangeFunctionAppSettings)
            Assert.Ignore($"Needs the test value of app setting {SettingKey}, but Azure:ChangeFunctionAppSettings is false");

        var setUpError = FunctionAppTestSettingsSwitcher.GetSetUpError(FunctionAppTestSettings.AppKeyOf(SettingKey));
        if (setUpError != null)
            Assert.Fail($"Needs the test value of app setting {SettingKey}, but setting it failed: {setUpError}");
    }

    public void AfterTest(ITest test)
    {
    }
}
