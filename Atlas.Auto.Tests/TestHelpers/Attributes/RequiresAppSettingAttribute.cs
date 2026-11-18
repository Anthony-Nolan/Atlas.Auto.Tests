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
/// The test is skipped when the run may not change app settings (Azure:ChangeFunctionAppSettings is false).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal class RequiresAppSettingAttribute(string settingKey) : PropertyAttribute(PropertyName, settingKey), ITestAction
{
    public const string PropertyName = "RequiresAppSetting";

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        if (!TestBase.Provider.GetRequiredService<AzureResourceSettings>().ChangeFunctionAppSettings)
            Assert.Ignore($"Needs the test value of app setting {settingKey}, but Azure:ChangeFunctionAppSettings is false");
    }

    public void AfterTest(ITest test)
    {
    }
}
