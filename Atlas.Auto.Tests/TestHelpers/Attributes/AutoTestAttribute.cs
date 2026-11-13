namespace Atlas.Auto.Tests.TestHelpers.Attributes;

/// <summary>
/// Links an automated test to its own Jira [Auto] test case, e.g. <c>[AutoTest("QA-2920")]</c>.
/// Shows up as the "AutoTest" property in test results and can be used in filters: <c>--filter "AutoTest=QA-2920"</c>.
/// </summary>
/// <remarks>
/// <see cref="ManualTestAttribute"/> names the manual case the test automates; this one names the test's own case.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal class AutoTestAttribute(string issueKey) : PropertyAttribute(PropertyName, issueKey)
{
    public const string PropertyName = "AutoTest";
}
