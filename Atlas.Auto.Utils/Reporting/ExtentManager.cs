using AventStack.ExtentReports;
using System.Collections.Concurrent;

namespace Atlas.Auto.Utils.Reporting;

public static class ExtentManager
{
    private static readonly ConcurrentDictionary<string, ExtentTest> TestFixtureMap = new();

    // Creates a parent node in the report for the test fixture
    public static ExtentTest CreateForFixture(string testFixtureName, string? description = null)
    {
        return TestFixtureMap.GetOrAdd(testFixtureName, ExtentService.Instance.CreateTest(testFixtureName, description));
    }

    // Creates a node in the report for the individual test
    public static ExtentTest CreateForTest(string testFixtureName, string testName, string? description = null)
    {
        return TestFixtureMap.TryGetValue(testFixtureName, out var testFixture)
            ? testFixture.CreateNode(testName, description)
            : throw new InvalidOperationException($"Test fixture {testFixtureName} does not exist in the test fixture map.");
    }
}