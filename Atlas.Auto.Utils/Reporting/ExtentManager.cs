using AventStack.ExtentReports;
using System.Collections.Concurrent;

namespace Atlas.Auto.Utils.Reporting
{
    public static class ExtentManager
    {
        private static ConcurrentDictionary<string, ExtentTest> parentTestMap = new ConcurrentDictionary<string, ExtentTest>();

        // Creates a parent node in the report for the test fixture
        public static ExtentTest CreateForFixture(string testFixtureName, string description = null)
        {
            var parentTest = ExtentService.Instance.CreateTest(testFixtureName, description);
            parentTestMap.TryAdd(testFixtureName, parentTest);
            return parentTest;
        }

        // Creates a node in the report for the individual test
        public static ExtentTest CreateForTest(string parentName, string testName, string description = null)
        {
            ExtentTest parentTest = null;
            if (!parentTestMap.ContainsKey(parentName))
            {
                parentTest = ExtentService.Instance.CreateTest(testName);
                parentTestMap.TryAdd(parentName, parentTest);
            }
            else
            {
                parentTest = parentTestMap[parentName];
            }
            return parentTest.CreateNode(testName, description);
        }
    }
}
