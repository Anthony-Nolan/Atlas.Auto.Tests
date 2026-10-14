namespace Atlas.Auto.Tests.TestHelpers.Attributes;

/// <summary>
/// Links an automated test to the Jira manual test it automates, e.g. <c>[ManualTest("QA-1926")]</c>.
/// Shows up as the "ManualTest" property in test results and can be used in filters: <c>--filter "ManualTest=QA-1926"</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal class ManualTestAttribute(string issueKey) : PropertyAttribute(PropertyName, issueKey)
{
    public const string PropertyName = "ManualTest";
}
